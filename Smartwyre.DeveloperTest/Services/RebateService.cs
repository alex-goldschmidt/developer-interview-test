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

    public RebateService(
        IRebateDataStore rebateDataStore,
        IProductDataStore productDataStore,
        IIncentiveCalculator fixedCashAmountCalculator,
        IIncentiveCalculator fixedRateRebateCalculator)
    {
        _rebateDataStore = rebateDataStore;
        _productDataStore = productDataStore;
        _fixedCashAmountCalculator = fixedCashAmountCalculator;
        _fixedRateRebateCalculator = fixedRateRebateCalculator;
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
                if (rebate == null)
                {
                    result.Success = false;
                }
                else if (product == null)
                {
                    result.Success = false;
                }
                else if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.AmountPerUom))
                {
                    result.Success = false;
                }
                else if (rebate.Amount == 0 || request.Volume == 0)
                {
                    result.Success = false;
                }
                else
                {
                    rebateAmount += rebate.Amount * request.Volume;
                    result.Success = true;
                }
                break;
        }

        if (result.Success)
        {
            _rebateDataStore.StoreCalculationResult(rebate, rebateAmount);
        }

        return result;
    }
}
