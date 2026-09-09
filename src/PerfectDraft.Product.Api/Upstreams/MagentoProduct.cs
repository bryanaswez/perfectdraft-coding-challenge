namespace PerfectDraft.Product.Api.Upstreams;

/// <summary>A product as Magento holds it.</summary>
public sealed record MagentoProduct
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public int Stock { get; init; }
}
