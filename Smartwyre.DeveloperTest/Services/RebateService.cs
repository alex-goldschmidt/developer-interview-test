using Smartwyre.DeveloperTest.Calculators;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Services;

public class RebateService : IRebateService
{
    private readonly IRebateDataStore _rebateDataStore;
    private readonly IProductDataStore _productDataStore;
    private readonly IIncentiveCalculator _fixedCashAmountCalculator;
    private readonly IIncentiveCalculator _fixedRateRebateCalculator;
    private readonly IIncentiveCalculator _amountPerUomCalculator;

    public RebateService(
        IRebateDataStore rebateDataStore,
        IProductDataStore productDataStore,
        IIncentiveCalculator fixedCashAmountCalculator,
        IIncentiveCalculator fixedRateRebateCalculator,
        IIncentiveCalculator amountPerUomCalculator)
    {
        _rebateDataStore = rebateDataStore;
        _productDataStore = productDataStore;
        _fixedCashAmountCalculator = fixedCashAmountCalculator;
        _fixedRateRebateCalculator = fixedRateRebateCalculator;
        _amountPerUomCalculator = amountPerUomCalculator;
    }

    public CalculateRebateResult Calculate(CalculateRebateRequest request)
    {
        var rebate = _rebateDataStore.GetRebate(request.RebateIdentifier);
        var product = _productDataStore.GetProduct(request.ProductIdentifier);

        var result = new CalculateRebateResult();

        var rebateAmount = 0m;

        switch (rebate.Incentive)
        {
            case IncentiveType.FixedCashAmount:
                var fixedCashAmount = _fixedCashAmountCalculator.Calculate(rebate, product, request);
                result.Success = fixedCashAmount.HasValue;
                rebateAmount = fixedCashAmount.GetValueOrDefault();
                break;

            case IncentiveType.FixedRateRebate:
                var fixedRateAmount = _fixedRateRebateCalculator.Calculate(rebate, product, request);
                result.Success = fixedRateAmount.HasValue;
                rebateAmount = fixedRateAmount.GetValueOrDefault();
                break;

            case IncentiveType.AmountPerUom:
                var amountPerUom = _amountPerUomCalculator.Calculate(rebate, product, request);
                result.Success = amountPerUom.HasValue;
                rebateAmount = amountPerUom.GetValueOrDefault();
                break;
        }

        if (result.Success)
        {
            _rebateDataStore.StoreCalculationResult(rebate, rebateAmount);
        }

        return result;
    }
}
