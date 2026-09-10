using Microsoft.AspNetCore.Mvc;
using PerfectDraft.Product.Api.Contracts;
using PerfectDraft.Product.Api.Upstreams;

namespace PerfectDraft.Product.Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductController : ControllerBase
{
    private readonly ProductDataFiles _dataFiles;

    public ProductController(ProductDataFiles dataFiles)
    {
        _dataFiles = dataFiles;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        // Magento decides which products exist, so the lookup starts there. A product that is only
        // in the search index has no Magento row and is therefore not found.
        var products = await _dataFiles.ReadMagentoProductsAsync(cancellationToken);
        var product = products.FirstOrDefault(p => p.Sku == id);

        if (product is null)
        {
            return NotFound();
        }

        var indexed = await _dataFiles.ReadSearchProductsAsync(cancellationToken);

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
            : products
                .Where(p => p.Name is not null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var indexed = await _dataFiles.ReadSearchProductsAsync(cancellationToken);

        return Ok(matches.Select(p => ProductResponse.From(p, FindEnrichment(indexed, p))).ToList());
    }

    /// <summary>Finds the index row for a Magento product, or null when the index has none.</summary>
    private static SearchProduct? FindEnrichment(List<SearchProduct> indexed, MagentoProduct product) =>
        indexed.FirstOrDefault(s => s.ObjectId == product.Sku);
}
