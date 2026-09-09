namespace PerfectDraft.Product.Api.Upstreams;

/// <summary>A row from the search index.</summary>
public sealed record SearchProduct
{
    /// <summary>The indexed product key, which corresponds to the Magento SKU.</summary>
    public string? ObjectId { get; init; }

    public string? Image { get; init; }
    public string? Title { get; init; }
    public decimal Price { get; init; }
    public bool InStock { get; init; }
}
