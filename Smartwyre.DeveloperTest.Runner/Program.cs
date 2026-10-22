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
        if (IsHelpRequested(args))
        {
            PrintUsage();
            return ExitCode.Success;
        }

        if (!TryParseArguments(args, out var request))
        {
            return ExitCode.InvalidInput;
        }

        var demoData = new DemoData();
        var rebate = demoData.RebateStore.GetRebate(request.RebateIdentifier);
        var product = demoData.ProductStore.GetProduct(request.ProductIdentifier);

        if (rebate == null)
        {
            Console.Error.WriteLine($"Unknown rebate '{request.RebateIdentifier}'. Use {HelpOption} to see the demo identifiers.");
            return ExitCode.InvalidInput;
        }

        if (product == null)
        {
            Console.Error.WriteLine($"Unknown product '{request.ProductIdentifier}'. Use {HelpOption} to see the demo identifiers.");
            return ExitCode.InvalidInput;
        }

        var service = new RebateService(
            demoData.RebateStore,
            demoData.ProductStore,
            new IIncentiveCalculator[]
            {
                new FixedCashAmountCalculator(),
                new FixedRateRebateCalculator(),
                new AmountPerUomCalculator()
            });

        request.RebateIdentifier = rebate.Identifier;
        request.ProductIdentifier = product.Identifier;

        var result = CalculateResult(service, request);
        DisplayResult(result, demoData.RebateStore.StoredCalculation, product, request.Volume);
        return result;
    }

    /// <summary>
    /// Recognizes no arguments or a single explicit help option as a help request.
    /// </summary>
    private static bool IsHelpRequested(string[] args) =>
        args.Length == 0
        || (args.Length == 1
            && (string.Equals(args[0], HelpOption, StringComparison.Ordinal)
                || string.Equals(args[0], ShortHelpOption, StringComparison.Ordinal)));

    /// <summary>
    /// Parses the required arguments into a request, displaying an error for invalid input.
    /// </summary>
    private static bool TryParseArguments(string[] args, out CalculateRebateRequest request)
    {
        request = null;

        if (args.Length != RequiredArgumentCount)
        {
            Console.Error.WriteLine("Expected a rebate identifier, product identifier, and volume.");
            PrintUsage();
            return false;
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
            return false;
        }

        request = new CalculateRebateRequest
        {
            RebateIdentifier = rebateIdentifier,
            ProductIdentifier = productIdentifier,
            Volume = volume
        };
        return true;
    }

    /// <summary>
    /// Runs the calculation and maps success, failure, or decimal overflow to a runner exit code.
    /// </summary>
    private static ExitCode CalculateResult(IRebateService service, CalculateRebateRequest request)
    {
        try
        {
            var result = service.Calculate(request);

            if (!result.Success)
            {
                return ExitCode.CalculationFailed;
            }

            return ExitCode.Success;
        }
        catch (OverflowException)
        {
            return ExitCode.InvalidInput;
        }
    }

    /// <summary>
    /// Displays the calculation outcome, reading the saved amount only after success.
    /// </summary>
    private static void DisplayResult(
        ExitCode result, RebateCalculation calculation, Product product, decimal volume)
    {
        if (result == ExitCode.InvalidInput)
        {
            Console.Error.WriteLine("The supplied volume makes this calculation exceed the supported decimal range.");
            return;
        }

        if (result == ExitCode.CalculationFailed)
        {
            Console.WriteLine("Calculation failed. Check the product's supported incentives and the rebate's required values.");
            return;
        }

        Console.WriteLine($"Incentive: {calculation.IncentiveType}");
        Console.WriteLine($"Product: {product.Identifier}");
        Console.WriteLine($"Volume: {volume.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Rebate amount: {calculation.Amount.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine("Calculation saved in memory for this run.");
    }

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
