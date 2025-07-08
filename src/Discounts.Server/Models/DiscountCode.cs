namespace Discounts.Server.Models;

public sealed class DiscountCode
{
    public string Code { get; set; } = default!;
    public bool Redeemed { get; set; }
    public Guid Version { get; set; } = default!;
}
