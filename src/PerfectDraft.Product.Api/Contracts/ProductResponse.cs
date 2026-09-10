using PerfectDraft.Product.Api.Upstreams;

namespace PerfectDraft.Product.Api.Contracts;

/// <summary>
/// The unified product view returned by the API. Magento supplies every commercial field. The
/// search index only adds display content that Magento does not hold.
/// </summary>
public sealed record ProductResponse
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    /// <summary>
    /// Derived from the Magento stock level. The exact quantity is not exposed, and the index has
    /// its own stock flag which is deliberately ignored because it lags Magento.
    /// </summary>
    public bool InStock { get; init; }

    /// <summary>From the search index, and null when the index holds no row for this product.</summary>
    public string? ImageUrl { get; init; }

    /// <param name="magento">The product as Magento holds it. Always required.</param>
    /// <param name="search">The matching index row, or null when there is no enrichment available.</param>
    public static ProductResponse From(MagentoProduct magento, SearchProduct? search) => new()
    {
        Sku = magento.Sku,
        Name = magento.Name,
        Price = magento.Price,
        Currency = magento.Currency,
        InStock = magento.Stock > 0,
        ImageUrl = search?.Image
    };
}
