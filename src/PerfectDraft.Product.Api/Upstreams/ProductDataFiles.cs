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

    public ProductDataFiles(IConfiguration configuration)
    {
        _magentoPath = Resolve(configuration["DataFiles:MagentoProductsPath"]);
        _searchPath = Resolve(configuration["DataFiles:SearchProductsPath"]);
    }

    public Task<List<MagentoProduct>> ReadMagentoProductsAsync(CancellationToken cancellationToken) =>
        ReadAsync<MagentoProduct>(_magentoPath, cancellationToken);

    public Task<List<SearchProduct>> ReadSearchProductsAsync(CancellationToken cancellationToken) =>
        ReadAsync<SearchProduct>(_searchPath, cancellationToken);

    private static string Resolve(string? configuredPath) =>
        Path.Combine(AppContext.BaseDirectory, configuredPath ?? string.Empty);

    private static async Task<List<T>> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, cancellationToken) ?? [];
    }
}
