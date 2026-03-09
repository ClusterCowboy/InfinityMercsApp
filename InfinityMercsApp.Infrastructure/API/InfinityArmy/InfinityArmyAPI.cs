namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http.Headers;

/// <inheritdoc/>
public sealed class InfinityArmyAPI(HttpClient httpClient, ILogger<InfinityArmyAPI> logger) : IInfinityArmyAPI
{
    private const string ArmyUrlBase = "https://api.corvusbelli.com/army/units/en/";
    private const string MetadataUrl = "https://api.corvusbelli.com/army/infinity/en/metadata";

    /// <inheritdoc/>
    public Task<string> GetMetaDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching metadata from: {Url}", MetadataUrl);
        return DownloadUrlAsync(MetadataUrl, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var fullUrl = $"{ArmyUrlBase}{factionId}";
        logger.LogInformation("Fetching army data from: {Url}", fullUrl);
        return DownloadUrlAsync(fullUrl, cancellationToken);
    }

    private async Task<string> DownloadUrlAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Origin", "https://infinitytheuniverse.com");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(80));

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed. Status={(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
        var contentEncodings = response.Content.Headers.ContentEncoding;

        if (contentEncodings.Any(e => e.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
        {
            await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync(timeoutCts.Token);
        }

        using var plainReader = new StreamReader(stream);
        return await plainReader.ReadToEndAsync(timeoutCts.Token);
    }
}
