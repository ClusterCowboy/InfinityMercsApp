namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

using InfinityMercsApp.Infrastructure.Models.API.Metadata;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <inheritdoc/>
public sealed class InfinityArmyAPI(HttpClient httpClient, ILogger<InfinityArmyAPI> logger) : IInfinityArmyAPI
{
    private const string ArmyUrlBase = "https://api.corvusbelli.com/army/units/en/";
    private const string MetadataUrl = "https://api.corvusbelli.com/army/infinity/en/metadata";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    /// <inheritdoc/>
    public async Task<MetadataDocument?> GetMetaDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching metadata from: {Url}", MetadataUrl);
        return JsonSerializer.Deserialize<MetadataDocument>(await GetAsync(MetadataUrl, cancellationToken), jsonOptions);
    }

    /// <inheritdoc/>
    public async Task<Models.API.Army.Faction?> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var fullUrl = $"{ArmyUrlBase}{factionId}";
        logger.LogInformation("Fetching army data from: {Url}", fullUrl);
        return JsonSerializer.Deserialize<Models.API.Army.Faction>(await GetAsync(fullUrl, cancellationToken));
    }

    private async Task<string> GetAsync(string url, CancellationToken cancellationToken)
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
