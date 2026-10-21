using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Calculators;

public interface IIncentiveCalculator
{
    IncentiveType Incentive { get; }

    /// <summary>
    /// Calculates the rebate amount, returning null when the calculation fails.
    /// </summary>
    decimal? Calculate(Rebate rebate, Product product, CalculateRebateRequest request);
}
