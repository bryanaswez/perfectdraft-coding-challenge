using System.Text.Json;

namespace PerfectDraft.Product.Api.Upstreams;

/// <summary>
/// Reads the two upstream JSON files. Paths come from the "DataFiles" configuration section and are
/// resolved against the application base directory, which is where the build copies data/*.json.
/// </summary>
public sealed class ProductDataFiles
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _magentoPath;
    private readonly string _searchPath;
    private readonly ILogger<ProductDataFiles> _logger;

    public ProductDataFiles(IConfiguration configuration, ILogger<ProductDataFiles> logger)
    {
        _magentoPath = Resolve(configuration["DataFiles:MagentoProductsPath"]);
        _searchPath = Resolve(configuration["DataFiles:SearchProductsPath"]);
        _logger = logger;
    }

    /// <summary>
    /// Reads the Magento catalogue, with unusable rows removed. Doing this on load rather than in
    /// the endpoints means both of them see the same catalogue and cannot drift apart.
    /// </summary>
    public async Task<List<MagentoProduct>> ReadMagentoProductsAsync(CancellationToken cancellationToken)
    {
        List<MagentoProduct> products;

        try
        {
            products = await ReadAsync<MagentoProduct>(_magentoPath, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Magento decides which products exist, so an unreadable catalogue means there is
            // nothing truthful to serve. The path is logged here and kept out of the response.
            _logger.LogError(exception, "Magento catalogue could not be read from {Path}.", _magentoPath);

            throw new UpstreamUnavailableException("Magento", exception);
        }

        return Usable(products);
    }

    public Task<List<SearchProduct>> ReadSearchProductsAsync(CancellationToken cancellationToken) =>
        ReadAsync<SearchProduct>(_searchPath, cancellationToken);

    /// <summary>
    /// Drops rows that cannot be served and resolves duplicate SKUs to one product each.
    /// </summary>
    /// <remarks>
    /// A row with no SKU cannot be looked up or ordered, so it is not a product this API can serve.
    /// Where a SKU repeats, the first row in the file wins. The rule matters less than having one:
    /// without it the price a caller sees depends on file ordering, and the single lookup and the
    /// collection could disagree. SKUs are compared without regard to case, matching how they are
    /// matched when a request supplies one.
    /// </remarks>
    private List<MagentoProduct> Usable(List<MagentoProduct> products)
    {
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usable = new List<MagentoProduct>(products.Count);

        var missingSku = 0;
        var duplicates = 0;

        foreach (var product in products)
        {
            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                missingSku++;
                continue;
            }

            if (!seenSkus.Add(product.Sku.Trim()))
            {
                duplicates++;
                continue;
            }

            usable.Add(product);
        }

        if (missingSku > 0)
        {
            _logger.LogWarning("Skipped {Count} Magento rows with no SKU.", missingSku);
        }

        if (duplicates > 0)
        {
            _logger.LogWarning("Skipped {Count} Magento rows with a duplicate SKU.", duplicates);
        }

        return usable;
    }

    private static string Resolve(string? configuredPath) =>
        Path.Combine(AppContext.BaseDirectory, configuredPath ?? string.Empty);

    private static async Task<List<T>> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, cancellationToken) ?? [];
    }
}
