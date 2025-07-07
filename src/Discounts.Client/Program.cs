using Discounts;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7042");
var client = new DiscountProvider.DiscountProviderClient(channel);

var shouldExit = false;
while (!shouldExit)
{
    var command = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(command))
    {
        continue;
    }

    var parts = command.Split(' ');
    switch (parts)
    {
        case ["generate", string count, string length]:
            var generateReply = await client.GenerateAsync(new GenerateRequest
            {
                Count = uint.Parse(count),
                Length = uint.Parse(length),
            });
            Console.WriteLine(generateReply.Result ? "Codes generated successfully." : "Failed to generate codes.");
            break;

        case ["use-code", string code]:
            var useCodeReply = await client.UseCodeAsync(new UseCodeRequest
            {
                Code = code,
            });
            break;

        case ["exist", ..]:
            shouldExit = true;
            break;

        default:
            Console.WriteLine("Unknown command.");
            break;
    }
}
