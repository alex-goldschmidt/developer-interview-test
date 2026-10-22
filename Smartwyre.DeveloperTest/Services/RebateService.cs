using System.Collections.Generic;
using System.Linq;
using Smartwyre.DeveloperTest.Calculators;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Services;

public class RebateService : IRebateService
{
    private readonly IRebateDataStore _rebateDataStore;
    private readonly IProductDataStore _productDataStore;
    private readonly IReadOnlyDictionary<IncentiveType, IIncentiveCalculator> _calculators;

    public RebateService(
        IRebateDataStore rebateDataStore,
        IProductDataStore productDataStore,
        IEnumerable<IIncentiveCalculator> calculators)
    {
        _rebateDataStore = rebateDataStore;
        _productDataStore = productDataStore;
        _calculators = calculators.ToDictionary(calculator => calculator.Incentive);
    }

    public CalculateRebateResult Calculate(CalculateRebateRequest request)
    {
        var rebate = _rebateDataStore.GetRebate(request.RebateIdentifier);
        var product = _productDataStore.GetProduct(request.ProductIdentifier);

        var result = new CalculateRebateResult();

        if (!_calculators.TryGetValue(rebate.Incentive, out var calculator))
        {
            return result;
        }

        var rebateAmount = calculator.Calculate(rebate, product, request);
        result.Success = rebateAmount.HasValue;

        if (result.Success)
        {
            _rebateDataStore.StoreCalculationResult(rebate, rebateAmount.Value);
        }

        return result;
    }
}
