using System;
using System.Collections.Generic;
using System.Linq;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

internal sealed class InMemoryRebateDataStore : IRebateDataStore
{
    private readonly IReadOnlyDictionary<string, Rebate> _rebates;

    public InMemoryRebateDataStore(IEnumerable<Rebate> rebates)
    {
        _rebates = rebates.ToDictionary(rebate => rebate.Identifier, StringComparer.OrdinalIgnoreCase);
    }

    public RebateCalculation StoredCalculation { get; private set; }

    public Rebate GetRebate(string rebateIdentifier)
    {
        var result = _rebates.GetValueOrDefault(rebateIdentifier);
        return result;
    }

    public void StoreCalculationResult(Rebate rebate, decimal rebateAmount)
    {
        StoredCalculation = new RebateCalculation
        {
            RebateIdentifier = rebate.Identifier,
            IncentiveType = rebate.Incentive,
            Amount = rebateAmount
        };
    }
}
