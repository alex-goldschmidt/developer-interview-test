using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Calculators;

public class FixedRateRebateCalculator : IIncentiveCalculator
{
    public IncentiveType Incentive => IncentiveType.FixedRateRebate;

    /// <summary>
    /// Calculates the fixed-rate rebate amount, returning null when the calculation fails.
    /// </summary>
    public decimal? Calculate(
        Rebate rebate, Product product, CalculateRebateRequest request)
    {
        if (product == null)
        {
            return null;
        }

        if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedRateRebate))
        {
            return null;
        }

        if (rebate.Percentage == 0 || product.Price == 0 || request.Volume == 0)
        {
            return null;
        }

        var result = product.Price * rebate.Percentage * request.Volume;
        return result;
    }
}
