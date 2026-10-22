using System;
using Smartwyre.DeveloperTest.Calculators;
using Smartwyre.DeveloperTest.Services;
using Smartwyre.DeveloperTest.Types;
using Xunit;
using static Smartwyre.DeveloperTest.Tests.RebateTestSupport;

namespace Smartwyre.DeveloperTest.Tests;

public class RebateServiceRegistrationTests
{
    private const IncentiveType AdditionalIncentive = (IncentiveType)100;

    [Theory]
    [InlineData(27.25)]
    [InlineData(0)]
    [InlineData(-27.25)]
    public void Calculate_AdditionalRegisteredIncentive_SavesCalculatorAmountOnce(decimal amount)
    {
        var rebate = CreateRebate(AdditionalIncentive);
        rebate.Amount = amount;
        var rebateStore = new FakeRebateDataStore(rebate);
        var productStore = new FakeProductDataStore(CreateProduct());
        var service = new RebateService(
            rebateStore,
            productStore,
            new IIncentiveCalculator[]
            {
                new FixedCashAmountCalculator(),
                new AdditionalIncentiveCalculator(),
                new FixedRateRebateCalculator(),
                new AmountPerUomCalculator()
            });

        var result = service.Calculate(CreateRequest());

        Assert.True(result.Success);
        AssertSavedCalculation(rebateStore, rebate, amount);
    }

    [Theory]
    [InlineData(IncentiveType.FixedCashAmount)]
    [InlineData(IncentiveType.FixedRateRebate)]
    [InlineData(IncentiveType.AmountPerUom)]
    public void Calculate_KnownIncentiveWithoutMatchingRegistration_ReturnsFailureWithoutPersisting(
        IncentiveType incentive)
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate(incentive));
        var service = new RebateService(
            rebateStore,
            new FakeProductDataStore(CreateProduct()),
            new IIncentiveCalculator[] { new AdditionalIncentiveCalculator() });

        var result = service.Calculate(CreateRequest());

        Assert.False(result.Success);
        Assert.Empty(rebateStore.SavedCalculations);
    }

    [Fact]
    public void Calculate_EmptyCalculatorRegistrations_ReturnsFailureWithoutPersisting()
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate(IncentiveType.FixedCashAmount));
        var service = new RebateService(
            rebateStore,
            new FakeProductDataStore(CreateProduct()),
            Array.Empty<IIncentiveCalculator>());

        var result = service.Calculate(CreateRequest());

        Assert.False(result.Success);
        Assert.Empty(rebateStore.SavedCalculations);
    }

    [Fact]
    public void Constructor_DuplicateIncentiveRegistrations_ThrowsBeforeAccessingStores()
    {
        var rebateStore = new FakeRebateDataStore(CreateRebate(AdditionalIncentive));
        var productStore = new FakeProductDataStore(CreateProduct());
        var calculators = new IIncentiveCalculator[]
        {
            new AdditionalIncentiveCalculator(),
            new AdditionalIncentiveCalculator()
        };

        Assert.Throws<ArgumentException>(() => new RebateService(rebateStore, productStore, calculators));

        Assert.Empty(rebateStore.RequestedIdentifiers);
        Assert.Empty(productStore.RequestedIdentifiers);
        Assert.Empty(rebateStore.SavedCalculations);
    }

    private sealed class AdditionalIncentiveCalculator : IIncentiveCalculator
    {
        public IncentiveType Incentive => AdditionalIncentive;

        public decimal? Calculate(Rebate rebate, Product product, CalculateRebateRequest request)
        {
            return rebate.Amount;
        }
    }
}
