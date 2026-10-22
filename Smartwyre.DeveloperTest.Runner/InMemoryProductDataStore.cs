using System;
using System.Collections.Generic;
using System.Linq;
using Smartwyre.DeveloperTest.Data;
using Smartwyre.DeveloperTest.Types;

namespace Smartwyre.DeveloperTest.Runner;

internal sealed class InMemoryProductDataStore : IProductDataStore
{
    private readonly IReadOnlyDictionary<string, Product> _products;

    public InMemoryProductDataStore(IEnumerable<Product> products)
    {
        _products = products.ToDictionary(product => product.Identifier, StringComparer.OrdinalIgnoreCase);
    }

    public Product GetProduct(string productIdentifier)
    {
        var result = _products.GetValueOrDefault(productIdentifier);
        return result;
    }
}
