using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

/// <summary>
/// Owns the sample records and in-memory stores for one runner invocation.
/// </summary>
internal sealed class DemoData
{
    public InMemoryRebateDataStore RebateStore { get; } = new(new[]
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

    public InMemoryProductDataStore ProductStore { get; } = new(new[]
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
}
