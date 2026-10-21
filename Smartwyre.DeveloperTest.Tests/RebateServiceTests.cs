using System;
using System.Collections.Generic;
using Smartwyre.DeveloperTest.Calculators;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;
using Xunit;

namespace Smartwyre.DeveloperTest.Tests;

// These tests describe current behavior, including defects to address in a later phase.
public class RebateServiceTests
{
    [Theory]
    [InlineData(IncentiveType.FixedCashAmount, 12.75)]
    [InlineData(IncentiveType.FixedRateRebate, 15.561875)]
    [InlineData(IncentiveType.AmountPerUom, 44.625)]
    public void Calculate_SupportedIncentive_SavesExactAmountOnce(
        IncentiveType incentive, decimal expectedAmount)
    {
        var rebate = CreateRebate(incentive);
        var rebateStore = new FakeRebateDataStore(rebate);
        var productStore = new FakeProductDataStore(CreateProduct());
        var service = CreateService(rebateStore, productStore);
        var request = CreateRequest();

        var result = service.Calculate(request);

        Assert.True(result.Success);
        AssertSavedCalculation(rebateStore, rebate, expectedAmount);
        Assert.Equal(request.RebateIdentifier, Assert.Single(rebateStore.RequestedIdentifiers));
        Assert.Equal(request.ProductIdentifier, Assert.Single(productStore.RequestedIdentifiers));
    }

    // Flags 0 through 7 cover every combination of the three supported incentive bits.
    [Theory]
    [InlineData(IncentiveType.FixedCashAmount, 0, false, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 1, false, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 2, false, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 3, false, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 4, true, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 5, true, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 6, true, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 7, true, 12.75)]
    [InlineData(IncentiveType.FixedRateRebate, 0, false, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 1, true, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 2, false, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 3, true, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 4, false, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 5, true, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 6, false, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, 7, true, 15.561875)]
    [InlineData(IncentiveType.AmountPerUom, 0, false, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 1, false, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 2, true, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 3, true, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 4, false, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 5, false, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 6, true, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 7, true, 44.625)]
    public void Calculate_IncentiveFlags_RequiresMatchingBit(
        IncentiveType incentive, int flags, bool expectedSuccess, decimal expectedAmount)
    {
        var rebate = CreateRebate(incentive);
        var product = CreateProduct();
        product.SupportedIncentives = (SupportedIncentiveType)flags;
        var rebateStore = new FakeRebateDataStore(rebate);
        var service = CreateService(rebateStore, new FakeProductDataStore(product));

        var result = service.Calculate(CreateRequest());

        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
            AssertSavedCalculation(rebateStore, rebate, expectedAmount);
        else
            Assert.Empty(rebateStore.SavedCalculations);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(12.75, true)]
    [InlineData(-12.75, true)]
    public void Calculate_FixedCashAmount_RejectsOnlyZeroAmount(decimal amount, bool expectedSuccess)
    {
        var rebate = CreateRebate(IncentiveType.FixedCashAmount);
        rebate.Amount = amount;
        AssertNumericOutcome(rebate, CreateProduct(), CreateRequest(), expectedSuccess, amount);
    }

    [Theory]
    [InlineData(20, 0.1, 3, true, 6)]
    [InlineData(0, 0.1, 3, false, 0)]
    [InlineData(20, 0, 3, false, 0)]
    [InlineData(20, 0.1, 0, false, 0)]
    [InlineData(-20, 0.1, 3, true, -6)]
    [InlineData(20, -0.1, 3, true, -6)]
    [InlineData(20, 0.1, -3, true, -6)]
    [InlineData(-20, -0.1, 3, true, 6)]
    [InlineData(-20, 0.1, -3, true, 6)]
    [InlineData(20, -0.1, -3, true, 6)]
    [InlineData(-20, -0.1, -3, true, -6)]
    [InlineData(20, 1.5, 3, true, 90)]
    public void Calculate_FixedRateRebate_RejectsZerosButAllowsNegativeFactorsAndRatesAboveOne(
        decimal price, decimal percentage, decimal volume, bool expectedSuccess, decimal expectedAmount)
    {
        var rebate = CreateRebate(IncentiveType.FixedRateRebate);
        rebate.Percentage = percentage;
        var product = CreateProduct();
        product.Price = price;
        var request = CreateRequest();
        request.Volume = volume;

        AssertNumericOutcome(rebate, product, request, expectedSuccess, expectedAmount);
    }

    [Theory]
    [InlineData(2, 3, true, 6)]
    [InlineData(0, 3, false, 0)]
    [InlineData(2, 0, false, 0)]
    [InlineData(-2, 3, true, -6)]
    [InlineData(2, -3, true, -6)]
    [InlineData(-2, -3, true, 6)]
    public void Calculate_AmountPerUom_RejectsZerosButAllowsNegativeFactors(
        decimal amount, decimal volume, bool expectedSuccess, decimal expectedAmount)
    {
        var rebate = CreateRebate(IncentiveType.AmountPerUom);
        rebate.Amount = amount;
        var request = CreateRequest();
        request.Volume = volume;

        AssertNumericOutcome(rebate, CreateProduct(), request, expectedSuccess, expectedAmount);
    }

    [Theory]
    [InlineData(IncentiveType.FixedCashAmount, 12.75, 0, 0, 0, 12.75)]
    [InlineData(IncentiveType.FixedCashAmount, 12.75, -1, -1, -1, 12.75)]
    [InlineData(IncentiveType.FixedRateRebate, 0, 0.125, 35.57, 3.5, 15.561875)]
    [InlineData(IncentiveType.FixedRateRebate, -1, 0.125, 35.57, 3.5, 15.561875)]
    [InlineData(IncentiveType.AmountPerUom, 12.75, 0, 0, 3.5, 44.625)]
    [InlineData(IncentiveType.AmountPerUom, 12.75, -1, -1, 3.5, 44.625)]
    public void Calculate_UnusedNumericFields_DoNotAffectSuccessOrAmount(
        IncentiveType incentive, decimal amount, decimal percentage,
        decimal price, decimal volume, decimal expectedAmount)
    {
        var rebate = CreateRebate(incentive);
        rebate.Amount = amount;
        rebate.Percentage = percentage;
        var product = CreateProduct();
        product.Price = price;
        var request = CreateRequest();
        request.Volume = volume;

        AssertNumericOutcome(rebate, product, request, true, expectedAmount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Calculate_MissingRebate_ThrowsNullReferenceAndDoesNotPersist(bool productAlsoMissing)
    {
        var rebateStore = new FakeRebateDataStore(null);
        var productStore = new FakeProductDataStore(productAlsoMissing ? null : CreateProduct());
        var service = CreateService(rebateStore, productStore);
        var request = CreateRequest();

        Assert.Throws<NullReferenceException>(() => service.Calculate(request));

        // Both lookups occur before rebate.Incentive is dereferenced.
        Assert.Equal(request.RebateIdentifier, Assert.Single(rebateStore.RequestedIdentifiers));
        Assert.Equal(request.ProductIdentifier, Assert.Single(productStore.RequestedIdentifiers));
        Assert.Empty(rebateStore.SavedCalculations);
    }

    [Theory]
    [InlineData(12.75)]
    [InlineData(0)]
    public void Calculate_FixedCashAmountWithMissingProduct_ThrowsEvenWhenAmountIsZero(decimal amount)
    {
        var rebate = CreateRebate(IncentiveType.FixedCashAmount);
        rebate.Amount = amount;
        var rebateStore = new FakeRebateDataStore(rebate);
        var service = CreateService(rebateStore, new FakeProductDataStore(null));

        Assert.Throws<NullReferenceException>(() => service.Calculate(CreateRequest()));

        Assert.Empty(rebateStore.SavedCalculations);
    }

    [Theory]
    [InlineData(IncentiveType.FixedRateRebate)]
    [InlineData(IncentiveType.AmountPerUom)]
    public void Calculate_OtherIncentivesWithMissingProduct_ReturnsFailureWithoutPersisting(IncentiveType incentive)
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate(incentive));
        var service = CreateService(rebateStore, new FakeProductDataStore(null));

        var result = service.Calculate(CreateRequest());

        Assert.False(result.Success);
        Assert.Empty(rebateStore.SavedCalculations);
    }

    [Fact]
    public void Calculate_NullRequest_ThrowsBeforeLookingUpRecordsOrPersisting()
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate(IncentiveType.FixedCashAmount));
        var productStore = new FakeProductDataStore(CreateProduct());
        var service = CreateService(rebateStore, productStore);

        Assert.Throws<NullReferenceException>(() => service.Calculate(null));

        Assert.Empty(rebateStore.RequestedIdentifiers);
        Assert.Empty(productStore.RequestedIdentifiers);
        Assert.Empty(rebateStore.SavedCalculations);
    }

    public static TheoryData<IncentiveType, string, bool, decimal> MissingIdentifiers
    {
        get
        {
            var data = new TheoryData<IncentiveType, string, bool, decimal>();
            var incentives = new (IncentiveType Incentive, decimal ExpectedAmount)[]
            {
                (IncentiveType.FixedCashAmount, 12.75m),
                (IncentiveType.FixedRateRebate, 15.561875m),
                (IncentiveType.AmountPerUom, 44.625m)
            };

            foreach (var (incentive, expectedAmount) in incentives)
            foreach (var identifier in new[] { null, "", " " })
            {
                data.Add(incentive, identifier, true, expectedAmount);
                data.Add(incentive, identifier, false, expectedAmount);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MissingIdentifiers))]
    public void Calculate_MissingIdentifier_IsForwardedToStoreAndSucceedsIfRecordsAreReturned(
        IncentiveType incentive, string identifier, bool missingRebateIdentifier, decimal expectedAmount)
    {
        var rebate = CreateRebate(incentive);
        var rebateStore = new FakeRebateDataStore(rebate);
        var productStore = new FakeProductDataStore(CreateProduct());
        var service = CreateService(rebateStore, productStore);
        var request = CreateRequest();
        if (missingRebateIdentifier)
            request.RebateIdentifier = identifier;
        else
            request.ProductIdentifier = identifier;

        var result = service.Calculate(request);

        Assert.True(result.Success);
        Assert.Equal(request.RebateIdentifier, Assert.Single(rebateStore.RequestedIdentifiers));
        Assert.Equal(request.ProductIdentifier, Assert.Single(productStore.RequestedIdentifiers));
        AssertSavedCalculation(rebateStore, rebate, expectedAmount);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(3, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(-1, true)]
    [InlineData(3, true)]
    [InlineData(int.MaxValue, true)]
    public void Calculate_UnknownIncentive_ReturnsFailureWithoutPersistingEvenIfProductIsMissing(
        int incentive, bool productMissing)
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate((IncentiveType)incentive));
        var productStore = new FakeProductDataStore(productMissing ? null : CreateProduct());
        var service = CreateService(rebateStore, productStore);
        var request = CreateRequest();

        var result = service.Calculate(request);

        Assert.False(result.Success);
        Assert.Equal(request.RebateIdentifier, Assert.Single(rebateStore.RequestedIdentifiers));
        Assert.Equal(request.ProductIdentifier, Assert.Single(productStore.RequestedIdentifiers));
        Assert.Empty(rebateStore.SavedCalculations);
    }

    private static void AssertNumericOutcome(
        Rebate rebate, Product product, CalculateRebateRequest request,
        bool expectedSuccess, decimal expectedAmount)
    {
        var rebateStore = new FakeRebateDataStore(rebate);
        var service = CreateService(rebateStore, new FakeProductDataStore(product));

        var result = service.Calculate(request);

        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
            AssertSavedCalculation(rebateStore, rebate, expectedAmount);
        else
            Assert.Empty(rebateStore.SavedCalculations);
    }

    private static RebateService CreateService(
        IRebateDataStore rebateStore, IProductDataStore productStore) =>
        new(rebateStore, productStore, new FixedCashAmountCalculator());

    private static Rebate CreateRebate(IncentiveType incentive) => new()
    {
        Identifier = "rebate-1",
        Incentive = incentive,
        Amount = 12.75m,
        Percentage = 0.125m
    };

    private static Product CreateProduct() => new()
    {
        Identifier = "product-1",
        Price = 35.57m,
        SupportedIncentives = SupportedIncentiveType.FixedCashAmount
            | SupportedIncentiveType.FixedRateRebate
            | SupportedIncentiveType.AmountPerUom
    };

    private static CalculateRebateRequest CreateRequest() => new()
    {
        RebateIdentifier = "rebate-1",
        ProductIdentifier = "product-1",
        Volume = 3.5m
    };

    private static void AssertSavedCalculation(
        FakeRebateDataStore store, Rebate expectedRebate, decimal expectedAmount)
    {
        var calculation = Assert.Single(store.SavedCalculations);
        Assert.Same(expectedRebate, calculation.Rebate);
        Assert.Equal(expectedAmount, calculation.Amount);
    }

    private sealed class FakeRebateDataStore : IRebateDataStore
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

    private sealed class FakeProductDataStore : IProductDataStore
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
}
