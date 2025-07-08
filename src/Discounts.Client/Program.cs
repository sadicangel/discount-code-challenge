using System.CommandLine;
using Discounts;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7222");
var client = new DiscountProvider.DiscountProviderClient(channel);


var root = new RootCommand("Discounts client implementation")
{
    CreateGenerateCommand(client),
    CreateUseCodeCommand(client),
    CreateRandomCodeCommand(client),
    CreateExitCommand()
};

root.Parse("-h").Invoke();

while (true)
{
    await root.Parse(Console.ReadLine() ?? string.Empty).InvokeAsync();
}

static Command CreateGenerateCommand(DiscountProvider.DiscountProviderClient client)
{
    var countOption = new Option<uint>("--count", "-c")
    {
        Required = true,
        Description = "The number of discount codes to generate",
    };
    var lengthOption = new Option<uint>("--length", "-l")
    {
        Required = true,
        Description = "The number of characters in a discount code (7 or 8)"
    };
    var command = new Command("generate", "Generate discount codes")
    {
        countOption,
        lengthOption
    };

    command.SetAction(async (parseResult, cancellationToken) =>
    {
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return;
        }

        var count = parseResult.GetValue(countOption);
        var length = parseResult.GetValue(lengthOption);

        var reply = await client.GenerateAsync(new GenerateRequest { Count = count, Length = length }, cancellationToken: cancellationToken);

        Console.WriteLine(reply.Result ? "Codes generated successfully." : "Failed to generate codes.");
    });

    return command;
}

static Command CreateUseCodeCommand(DiscountProvider.DiscountProviderClient client)
{
    var codeArgument = new Argument<string>("Code")
    {
        Description = "Discount code to be redeemed",
    };

    var command = new Command("use-code", "Redeem a discount code")
    {
        codeArgument
    };

    command.SetAction(async (parseResult, cancellationToken) =>
    {
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return;
        }

        var code = parseResult.GetValue(codeArgument);

        var reply = await client.UseCodeAsync(new UseCodeRequest { Code = code }, cancellationToken: cancellationToken);
    });

    return command;
}

static Command CreateRandomCodeCommand(DiscountProvider.DiscountProviderClient client)
{
    var command = new Command("random", "Retrieves a random code from previously generated codes");
    command.SetAction(async (parseResult, cancellationToken) =>
    {
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return;
        }

        var reply = await client.RandomCodeAsync(new RandomCodeRequest(), cancellationToken: cancellationToken);

        Console.WriteLine(string.IsNullOrWhiteSpace(reply.Code) ? "No codes have been generated" : $"Discount code: {reply.Code}");
    });
    return command;
}

static Command CreateExitCommand()
{
    var command = new Command("exit", "Exit the application");
    command.SetAction(x => Environment.Exit(0));
    return command;
}
