using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Server.Services;

public sealed class DiscountCodeGenerator(AppDbContext dbContext, ILogger<DiscountCodeGenerator> logger)
{
    private static readonly string s_commaNewLine = $",{Environment.NewLine}";
    private static readonly string s_base36Chars = "0123456789abcdefghijklmnopqrstuvwxyz";

    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "We own the data - too lazy to grab dapper :)")]
    public async Task<bool> GenerateCodesAsync(int count, int length, CancellationToken cancellationToken = default)
    {
        var totalCount = 0;

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

        try
        {
            while (totalCount < count)
            {
                var iterationCount = await dbContext.Database.ExecuteSqlRawAsync(
                    $"""
                    INSERT OR IGNORE INTO discount_codes (code, version) VALUES
                    {string.Join(s_commaNewLine, GenerateDiscountCodeRows(count - totalCount, length))};
                    """,
                    cancellationTokenSource.Token);

                logger.LogInformation("Generated {@Count} discount codes", iterationCount);

                totalCount += iterationCount;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Code generation failed due to timeout");
        }

        return totalCount == count;
    }

    private static string[] GenerateDiscountCodeRows(int count, int length)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        Span<char> buffer = stackalloc char[1 + length + 2 + 2 + 36 + 2 + 1];

        buffer[0] = '(';
        buffer[^1] = ')';
        buffer[1 + length + 2] = ',';
        buffer[1 + length + 3] = ' ';

        var codeBuffer = buffer.Slice(1, length + 2);
        codeBuffer[0] = '\'';
        codeBuffer[^1] = '\'';
        codeBuffer = codeBuffer.Slice(1, length);

        var guidBuffer = buffer.Slice(1 + length + 2 + 2, 36 + 2);
        guidBuffer[0] = '\'';
        guidBuffer[^1] = '\'';
        guidBuffer = guidBuffer.Slice(1, 36);

        var discountCodes = new string[count];
        for (var i = 0; i < count; ++i)
        {
            RandomNumberGenerator.GetItems(s_base36Chars, codeBuffer);
            if (!Guid.CreateVersion7().TryFormat(guidBuffer, out var a))
                throw new UnreachableException($"Failed to generate GUID");

            discountCodes[i] = buffer.ToString();
        }
        return discountCodes;
    }
}
