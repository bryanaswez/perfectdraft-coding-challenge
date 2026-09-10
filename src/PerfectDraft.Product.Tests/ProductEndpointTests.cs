using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PerfectDraft.Product.Api.Contracts;
using Xunit;

namespace PerfectDraft.Product.Tests;

/// <summary>
/// Drives the real API over HTTP against the real fixture files, so routing, status codes and the
/// aggregation are covered end to end.
/// </summary>
public class ProductEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_by_id_returns_magento_commercials_and_the_indexed_image()
    {
        var response = await _client.GetAsync("/products/P100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal(38.50m, product!.Price);  // the index says 42.79
        Assert.True(product.InStock);          // the index says out of stock
        Assert.Equal("GBP", product.Currency);
        Assert.Contains("09dba87f", product.ImageUrl);
    }

    [Fact]
    public async Task Get_by_id_returns_not_found_for_a_product_that_only_exists_in_the_index()
    {
        var response = await _client.GetAsync("/products/P999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_not_found_for_an_unknown_id()
    {
        var response = await _client.GetAsync("/products/ZZZZ");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_a_product_that_the_index_does_not_hold()
    {
        var response = await _client.GetAsync("/products/M100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Null(product!.ImageUrl);
        Assert.Equal(25.00m, product.Price);
    }

    [Fact]
    public async Task Search_without_a_term_returns_every_magento_product()
    {
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/products");

        Assert.NotNull(products);
        Assert.Equal(6, products!.Count);
    }

    [Fact]
    public async Task Search_matches_the_product_name_regardless_of_case()
    {
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/products?search=STELLA");

        Assert.NotNull(products);
        Assert.Equal("P100", Assert.Single(products!).Sku);
    }

    [Fact]
    public async Task Search_with_no_matches_is_an_empty_list_and_not_a_not_found()
    {
        var response = await _client.GetAsync("/products?search=xyzzy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();

        Assert.NotNull(products);
        Assert.Empty(products!);
    }

    [Fact]
    public async Task Search_never_returns_a_product_that_only_exists_in_the_index()
    {
        // "Camden Rugby Ball" is in the search index only, so searching its name finds nothing.
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/products?search=rugby");

        Assert.NotNull(products);
        Assert.Empty(products!);
    }
}
