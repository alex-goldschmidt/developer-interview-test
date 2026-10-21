using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Calculators;

public class FixedCashAmountCalculator : IIncentiveCalculator
{
    public IncentiveType Incentive => IncentiveType.FixedCashAmount;

    public decimal? Calculate(
        Rebate rebate, Product product, CalculateRebateRequest request)
    {
        if (!product.SupportedIncentives.HasFlag(SupportedIncentiveType.FixedCashAmount))
        {
            return null;
        }

        if (rebate.Amount == 0)
        {
            return null;
        }

        return rebate.Amount;
    }
}
