using System;
using System.Globalization;
using Smartwyre.DeveloperTest.Calculators;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

class Program
{
    private const int RequiredArgumentCount = 3;
    private const string HelpOption = "--help";
    private const string ShortHelpOption = "-h";

    /// <summary>
    /// Converts the runner outcome to the integer exit code required by the process entry point.
    /// </summary>
    static int Main(string[] args) => (int)Run(args);

    private static ExitCode Run(string[] args)
    {
        var isHelpRequested = args.Length == 1
            && (string.Equals(args[0], HelpOption, StringComparison.Ordinal)
                || string.Equals(args[0], ShortHelpOption, StringComparison.Ordinal));

        if (args.Length == 0 || isHelpRequested)
        {
            PrintUsage();
            return ExitCode.Success;
        }

        if (args.Length != RequiredArgumentCount)
        {
            Console.Error.WriteLine("Expected a rebate identifier, product identifier, and volume.");
            PrintUsage();
            return ExitCode.InvalidInput;
        }

        var rebateIdentifier = args[0];
        var productIdentifier = args[1];
        var volumeInput = args[2];

        if (!decimal.TryParse(
                volumeInput,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var volume))
        {
            Console.Error.WriteLine("Volume must be a decimal number using '.' as the decimal separator.");
            return ExitCode.InvalidInput;
        }

        var rebateStore = CreateRebateStore();
        var productStore = CreateProductStore();
        var rebate = rebateStore.GetRebate(rebateIdentifier);
        var product = productStore.GetProduct(productIdentifier);

        if (rebate == null)
        {
            Console.Error.WriteLine($"Unknown rebate '{rebateIdentifier}'. Use {HelpOption} to see the demo identifiers.");
            return ExitCode.InvalidInput;
        }

        if (product == null)
        {
            Console.Error.WriteLine($"Unknown product '{productIdentifier}'. Use {HelpOption} to see the demo identifiers.");
            return ExitCode.InvalidInput;
        }

        var service = new RebateService(
            rebateStore,
            productStore,
            new IIncentiveCalculator[]
            {
                new FixedCashAmountCalculator(),
                new FixedRateRebateCalculator(),
                new AmountPerUomCalculator()
            });

        var request = new CalculateRebateRequest
        {
            RebateIdentifier = rebate.Identifier,
            ProductIdentifier = product.Identifier,
            Volume = volume
        };

        CalculateRebateResult result;
        try
        {
            result = service.Calculate(request);
        }
        catch (OverflowException)
        {
            Console.Error.WriteLine("The supplied volume makes this calculation exceed the supported decimal range.");
            return ExitCode.InvalidInput;
        }

        if (!result.Success)
        {
            Console.WriteLine("Calculation failed. Check the product's supported incentives and the rebate's required values.");
            return ExitCode.CalculationFailed;
        }

        Console.WriteLine($"Incentive: {rebate.Incentive}");
        Console.WriteLine($"Product: {product.Identifier}");
        Console.WriteLine($"Volume: {volume.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Rebate amount: {rebateStore.StoredCalculation.Amount.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine("Calculation saved in memory for this run.");
        return ExitCode.Success;
    }

    private static InMemoryRebateDataStore CreateRebateStore() => new(new[]
    {
        new Rebate
        {
            Identifier = "cash",
            Incentive = IncentiveType.FixedCashAmount,
            Amount = 10m
        },
        new Rebate
        {
            Identifier = "rate",
            Incentive = IncentiveType.FixedRateRebate,
            Percentage = 0.1m
        },
        new Rebate
        {
            Identifier = "per-uom",
            Incentive = IncentiveType.AmountPerUom,
            Amount = 2.5m
        }
    });

    private static InMemoryProductDataStore CreateProductStore() => new(new[]
    {
        new Product
        {
            Identifier = "product-1",
            Price = 20m,
            Uom = "unit",
            SupportedIncentives = SupportedIncentiveType.FixedCashAmount
                | SupportedIncentiveType.FixedRateRebate
                | SupportedIncentiveType.AmountPerUom
        },
        new Product
        {
            Identifier = "unsupported-product",
            Price = 20m,
            Uom = "unit",
            SupportedIncentives = (SupportedIncentiveType)0
        }
    });

    private static void PrintUsage()
    {
        Console.WriteLine($"""
            Usage:
              dotnet run --project Smartwyre.DeveloperTest.Runner -- <rebate> <product> <volume>

            Demo rebates:
              cash       Fixed cash amount of 10
              rate       Fixed rate of 0.1 (10%)
              per-uom    Amount of 2.5 per unit

            Demo products:
              product-1            Price 20 per unit; supports all three incentives
              unsupported-product  Price 20 per unit; supports no incentives

            Examples:
              dotnet run --project Smartwyre.DeveloperTest.Runner -- cash product-1 3
              dotnet run --project Smartwyre.DeveloperTest.Runner -- rate product-1 3
              dotnet run --project Smartwyre.DeveloperTest.Runner -- per-uom product-1 3.5
              dotnet run --project Smartwyre.DeveloperTest.Runner -- rate unsupported-product 3

            Use '.' for decimal volumes. Identifiers are case-insensitive.
            Use {HelpOption} or {ShortHelpOption} to show this help.
            Calculations are saved in memory for this run.
            Exit codes:
              {(int)ExitCode.Success} = success/help
              {(int)ExitCode.CalculationFailed} = unsuccessful calculation
              {(int)ExitCode.InvalidInput} = invalid input
            """);
    }
}
