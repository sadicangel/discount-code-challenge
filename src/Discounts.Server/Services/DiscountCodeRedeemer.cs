using Discounts.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Server.Services;

public sealed class DiscountCodeRedeemer(AppDbContext dbContext, ILogger<DiscountCodeRedeemer> logger)
{
    public async Task<UseCodeResult> RedeemCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var discountCode = await dbContext.FindAsync<DiscountCode>(code);
        if (discountCode is null)
        {
            logger.LogWarning("Discount code {@Code} does not exist", code);
            return UseCodeResult.NotFound;
        }

        if (discountCode.Redeemed)
        {
            logger.LogInformation("Discount code {@Code} already redeemed", code);
            return UseCodeResult.AlreadyRedeemed;
        }

        discountCode.Redeemed = true;
        discountCode.Version = Guid.NewGuid();

        try
        {
            // TODO: Fix concurrency here.
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Redeemed code {@Code}", code);
            return UseCodeResult.Redeemed;
        }
        catch (DbUpdateException)
        {
            logger.LogWarning("Concurrency error while redeeming code {@Code}", code);
            return UseCodeResult.AlreadyRedeemed;
        }
    }
}
