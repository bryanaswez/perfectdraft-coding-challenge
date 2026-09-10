using PerfectDraft.Product.Api.Contracts;
using PerfectDraft.Product.Api.Upstreams;
using Xunit;

namespace PerfectDraft.Product.Tests;

/// <summary>
/// Pins the merge rules. These run against the mapping directly, with no host and no files, so the
/// precedence decisions are stated as executable rules rather than left to whoever edits next.
/// </summary>
public class ProductResponseTests
{
    private static MagentoProduct Magento(decimal price = 38.50m, int stock = 12) => new()
    {
        Sku = "P100",
        Name = "PerfectDraft Stella Artois 6L Keg",
        Price = price,
        Currency = "GBP",
        Stock = stock
    };

    private static SearchProduct Indexed(decimal price = 42.79m, bool inStock = false) => new()
    {
        ObjectId = "P100",
        Title = "Stella Artois",
        Image = "https://example.test/p100.webp",
        Price = price,
        InStock = inStock
    };

    [Fact]
    public void Magento_price_wins_over_the_index_price()
    {
        var result = ProductResponse.From(Magento(price: 38.50m), Indexed(price: 42.79m));

        Assert.Equal(38.50m, result.Price);
    }

    [Fact]
    public void Currency_comes_from_magento()
    {
        var result = ProductResponse.From(Magento(), Indexed());

        Assert.Equal("GBP", result.Currency);
    }

    [Fact]
    public void Index_out_of_stock_flag_is_ignored_when_magento_holds_stock()
    {
        // P100 in the fixtures: Magento holds 12 units while the index claims out of stock.
        var result = ProductResponse.From(Magento(stock: 12), Indexed(inStock: false));

        Assert.True(result.InStock);
    }

    [Fact]
    public void Zero_magento_stock_is_out_of_stock_whatever_the_index_says()
    {
        var result = ProductResponse.From(Magento(stock: 0), Indexed(inStock: true));

        Assert.False(result.InStock);
    }

    [Fact]
    public void Image_is_taken_from_the_index()
    {
        var result = ProductResponse.From(Magento(), Indexed());

        Assert.Equal("https://example.test/p100.webp", result.ImageUrl);
    }

    [Fact]
    public void Product_without_an_index_row_keeps_its_magento_fields_and_has_no_image()
    {
        var result = ProductResponse.From(Magento(), search: null);

        Assert.Null(result.ImageUrl);
        Assert.Equal("PerfectDraft Stella Artois 6L Keg", result.Name);
        Assert.Equal(38.50m, result.Price);
        Assert.True(result.InStock);
    }
}
