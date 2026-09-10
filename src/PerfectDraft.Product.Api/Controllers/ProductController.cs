using Microsoft.AspNetCore.Mvc;
using PerfectDraft.Product.Api.Contracts;
using PerfectDraft.Product.Api.Upstreams;

namespace PerfectDraft.Product.Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductController : ControllerBase
{
    private readonly ProductDataFiles _dataFiles;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductDataFiles dataFiles, ILogger<ProductController> logger)
    {
        _dataFiles = dataFiles;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        // Magento decides which products exist, so the lookup starts there. A product that is only
        // in the search index has no Magento row and is therefore not found.
        var products = await _dataFiles.ReadMagentoProductsAsync(cancellationToken);
        // Ids arrive from a URL, where casing is not something a caller reliably controls.
        var product = products.FirstOrDefault(p => string.Equals(p.Sku, id, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            return NotFound();
        }

        var indexed = await ReadEnrichmentAsync(cancellationToken);

        return Ok(ProductResponse.From(product, FindEnrichment(indexed, product)));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var products = await _dataFiles.ReadMagentoProductsAsync(cancellationToken);

        // No term means the whole collection. A term that matches nothing is still a successful
        // search, so it returns an empty list rather than a 404.
        var matches = string.IsNullOrWhiteSpace(search)
            ? products
            : products.Where(p => Matches(p, search.Trim())).ToList();

        var indexed = await ReadEnrichmentAsync(cancellationToken);

        return Ok(matches.Select(p => ProductResponse.From(p, FindEnrichment(indexed, p))).ToList());
    }

    /// <summary>
    /// Decides whether a product matches a search term.
    /// </summary>
    /// <remarks>
    /// The name is matched on a substring, because that is how someone searches for a product.
    /// The SKU is matched whole, because a product code is an identifier that a caller pastes in
    /// rather than types part of, and matching it on a substring would let a short term pull back
    /// unrelated products.
    /// </remarks>
    private static bool Matches(MagentoProduct product, string term) =>
        (product.Name is not null && product.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
        || string.Equals(product.Sku, term, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the search index, which is optional. It supplies display content that Magento does not
    /// hold, so a failure costs the caller an image rather than the product itself.
    /// </summary>
    /// <remarks>
    /// The catch is deliberately broad. Any failure of an optional dependency should degrade rather
    /// than propagate, and narrowing it to specific exception types would only mean the next
    /// unanticipated one takes down a request that Magento has already answered. Cancellation is
    /// excluded because that is the caller going away, not the index failing.
    /// </remarks>
    private async Task<List<SearchProduct>> ReadEnrichmentAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dataFiles.ReadSearchProductsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Search index could not be read. Serving products without enrichment.");

            return [];
        }
    }

    /// <summary>Finds the index row for a Magento product, or null when the index has none.</summary>
    private static SearchProduct? FindEnrichment(List<SearchProduct> indexed, MagentoProduct product) =>
        indexed.FirstOrDefault(s => string.Equals(s.ObjectId, product.Sku, StringComparison.OrdinalIgnoreCase));
}
