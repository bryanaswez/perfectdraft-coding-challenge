namespace PerfectDraft.Product.Api.Upstreams;

/// <summary>
/// Thrown when an upstream system this API depends on cannot be read. It distinguishes a dependency
/// being unavailable, which may be temporary, from a fault in the request itself.
/// </summary>
public sealed class UpstreamUnavailableException : Exception
{
    public UpstreamUnavailableException(string upstream, Exception innerException)
        : base($"The {upstream} upstream could not be read.", innerException)
    {
        Upstream = upstream;
    }

    /// <summary>The upstream that failed, for logging. Never returned to a caller.</summary>
    public string Upstream { get; }
}
