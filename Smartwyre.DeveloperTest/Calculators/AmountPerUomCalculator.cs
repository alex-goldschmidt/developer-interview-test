using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Calculators;

public class AmountPerUomCalculator : IIncentiveCalculator
{
    public IncentiveType Incentive => IncentiveType.AmountPerUom;

    /// <summary>
    /// Calculates the amount-per-unit rebate, returning null when the calculation fails.
    /// </summary>
    public decimal? Calculate(
        Rebate rebate, Product product, CalculateRebateRequest request)
    {
        if (product == null)
        {
            return null;
        }

        if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.AmountPerUom))
        {
            return null;
        }

        if (rebate.Amount == 0 || request.Volume == 0)
        {
            return null;
        }

        var result = rebate.Amount * request.Volume;
        return result;
    }
}
