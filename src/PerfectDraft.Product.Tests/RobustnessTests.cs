 using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PerfectDraft.Product.Api.Contracts;
using Xunit;

namespace PerfectDraft.Product.Tests;

/// <summary>
/// Covers what the API does when an upstream is unavailable or the data it holds is inconsistent.
/// </summary>
/// <remarks>
/// Each of these was written first, as a failing specification of the behaviour the API ought to
/// have, and the code was then changed until it passed. The comments state the reasoning rather
/// than the mechanics, because the reasoning is the part that is not obvious from the assertion.
/// </remarks>
public class RobustnessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RobustnessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>Runs the API with an upstream file path pointed somewhere else.</summary>
    private HttpClient ClientWith(string settingKey, string path) =>
        _factory
            .WithWebHostBuilder(builder => builder.UseSetting(settingKey, path))
            .CreateClient();

    // The search index supplies images only. Losing it should cost the caller an image, not the
    // product. Magento is read first and already holds every field needed to answer.
    [Fact]
    public async Task An_unavailable_search_index_still_serves_the_magento_product()
    {
        var client = ClientWith("DataFiles:SearchProductsPath", "TestData/no-such-file.json");

        var response = await client.GetAsync("/products/P100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal(38.50m, product!.Price);
        Assert.True(product.InStock);
        Assert.Null(product.ImageUrl);
    }

    // The same rule applies to the collection endpoint.
    [Fact]
    public async Task An_unavailable_search_index_still_serves_the_full_collection()
    {
        var client = ClientWith("DataFiles:SearchProductsPath", "TestData/no-such-file.json");

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/products");

        Assert.NotNull(products);
        Assert.Equal(6, products!.Count);
        Assert.All(products, p => Assert.Null(p.ImageUrl));
    }

    // Magento decides which products exist, so an unreadable Magento file means there is no
    // catalogue and nothing truthful to serve. That is a dependency failure rather than a bug in
    // the request, and it may be temporary, so 503 is the honest answer rather than 500.
    [Fact]
    public async Task Unreadable_magento_data_is_reported_as_a_dependency_failure()
    {
        var client = ClientWith("DataFiles:MagentoProductsPath", "TestData/magento-malformed.json");

        var response = await client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // The response must not depend on the hosting environment, and must never hand a caller the
    // exception type or a stack trace. Today the developer exception page does exactly that
    // whenever the app runs in Development.
    [Fact]
    public async Task Upstream_failures_do_not_leak_internals_to_the_caller()
    {
        var client = ClientWith("DataFiles:MagentoProductsPath", "TestData/magento-malformed.json");

        var response = await client.GetAsync("/products");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("JsonException", body);
        Assert.DoesNotContain("ProductDataFiles", body);
    }

    // A row with no SKU cannot be looked up or ordered, so it is not a product the API can serve.
    // The fixture holds one unusable row and one good one.
    [Fact]
    public async Task Rows_without_a_sku_are_not_served()
    {
        var client = ClientWith("DataFiles:MagentoProductsPath", "TestData/magento-null-sku.json");

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/products");

        Assert.NotNull(products);
        Assert.Equal("P100", Assert.Single(products!).Sku);
    }

    // Two rows share a SKU at different prices. One product means one entry, chosen by a stated
    // rule rather than by file ordering. First row wins, so repeated loads are deterministic and
    // the single lookup and the collection cannot disagree.
    [Fact]
    public async Task Duplicate_skus_are_resolved_to_a_single_product()
    {
        var client = ClientWith("DataFiles:MagentoProductsPath", "TestData/magento-duplicate-sku.json");

        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/products");

        Assert.NotNull(products);

        var product = Assert.Single(products!);

        Assert.Equal("P100", product.Sku);
        Assert.Equal(1.00m, product.Price);
        Assert.Equal("First Row", product.Name);
    }

    // Ids arrive from a URL, where casing is not something a caller reliably controls.
    [Fact]
    public async Task Product_ids_are_matched_regardless_of_case()
    {
        var response = await _factory.CreateClient().GetAsync("/products/p100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal("P100", product!.Sku);
    }

    // Pasting a product code into a search box is normal on a retail site, so the SKU is matched
    // as well as the name. The SKU is matched whole rather than on a substring, so that a short
    // term cannot pull back unrelated products.
    [Fact]
    public async Task Search_also_matches_on_sku()
    {
        var products = await _factory.CreateClient()
            .GetFromJsonAsync<List<ProductResponse>>("/products?search=P100");

        Assert.NotNull(products);
        Assert.Equal("P100", Assert.Single(products!).Sku);
    }
}
