using Microsoft.EntityFrameworkCore;

namespace Discounts.Server.Services;

public sealed class DiscountCodeRedeemer(AppDbContext dbContext, ILogger<DiscountCodeRedeemer> logger)
{
    public async Task<UseCodeResult> RedeemCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var discountCode = await dbContext.DiscountCodes.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (discountCode is null)
        {
            logger.LogWarning("Discount code '{@Code}' does not exist", code);
            return UseCodeResult.NotFound;
        }

        if (discountCode.Redeemed)
        {
            logger.LogInformation("Discount code '{@Code}' already redeemed", code);
            return UseCodeResult.AlreadyRedeemed;
        }

        discountCode.Redeemed = true;
        discountCode.Version = Guid.CreateVersion7();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Redeemed discount code '{@Code}'", code);
            return UseCodeResult.Redeemed;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrency error while redeeming discount code '{@Code}'", code);
            return UseCodeResult.AlreadyRedeemed;
        }
    }
}
