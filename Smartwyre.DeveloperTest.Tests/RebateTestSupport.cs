using System.Collections.Generic;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;
using Xunit;

namespace Smartwyre.DeveloperTest.Tests;

internal static class RebateTestSupport
{
    internal static Rebate CreateRebate(IncentiveType incentive) => new()
    {
        Identifier = "rebate-1",
        Incentive = incentive,
        Amount = 12.75m,
        Percentage = 0.125m
    };

    internal static Product CreateProduct() => new()
    {
        Identifier = "product-1",
        Price = 35.57m,
        SupportedIncentives = SupportedIncentiveType.FixedCashAmount
            | SupportedIncentiveType.FixedRateRebate
            | SupportedIncentiveType.AmountPerUom
    };

    internal static CalculateRebateRequest CreateRequest() => new()
    {
        RebateIdentifier = "rebate-1",
        ProductIdentifier = "product-1",
        Volume = 3.5m
    };

    internal static void AssertSavedCalculation(
        FakeRebateDataStore store, Rebate expectedRebate, decimal expectedAmount)
    {
        var calculation = Assert.Single(store.SavedCalculations);
        Assert.Same(expectedRebate, calculation.Rebate);
        Assert.Equal(expectedAmount, calculation.Amount);
    }
}

internal sealed class FakeRebateDataStore : IRebateDataStore
{
    private readonly Rebate rebate;

    public FakeRebateDataStore(Rebate rebate) => this.rebate = rebate;

    public List<string> RequestedIdentifiers { get; } = new();
    public List<(Rebate Rebate, decimal Amount)> SavedCalculations { get; } = new();

    public Rebate GetRebate(string rebateIdentifier)
    {
        RequestedIdentifiers.Add(rebateIdentifier);
        return rebate;
    }

    public void StoreCalculationResult(Rebate rebate, decimal rebateAmount)
    {
        SavedCalculations.Add((rebate, rebateAmount));
    }
}

internal sealed class FakeProductDataStore : IProductDataStore
{
    private readonly Product product;

    public FakeProductDataStore(Product product) => this.product = product;

    public List<string> RequestedIdentifiers { get; } = new();

    public Product GetProduct(string productIdentifier)
    {
        RequestedIdentifiers.Add(productIdentifier);
        return product;
    }
}
