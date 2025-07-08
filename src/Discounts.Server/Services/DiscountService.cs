using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Server.Services;

public sealed class DiscountService(DiscountCodeGenerator generator, DiscountCodeRedeemer redeemer, AppDbContext dbContext) : DiscountProvider.DiscountProviderBase
{
    public override async Task<GenerateReply> Generate(GenerateRequest request, ServerCallContext context)
    {
        if (request.Count > 2000)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "2000 discount codes limit exceeded"));

        if (request.Length is not (7 or 8))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Discount codes must have 7 or 8 characters"));

        var result = await generator.GenerateCodesAsync((int)request.Count, (int)request.Length, context.CancellationToken);

        return new GenerateReply { Result = result };
    }

    public override async Task<UseCodeReply> UseCode(UseCodeRequest request, ServerCallContext context)
    {
        if (request.Code is not { Length: 7 or 8 })
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Discount codes must have 7 or 8 characters"));

        var result = await redeemer.RedeemCodeAsync(request.Code, context.CancellationToken);

        return new UseCodeReply { Result = result };
    }

    public override async Task<RandomCodeReply> RandomCode(RandomCodeRequest request, ServerCallContext context)
    {
        var row = await dbContext.DiscountCodes.OrderBy(x => x.Version).FirstOrDefaultAsync();

        return new RandomCodeReply { Code = row?.Code ?? string.Empty };
    }
}
