using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.Services;

public sealed class StoreProvider : IStoreProvider
{
    private static readonly IReadOnlyDictionary<string, string> StoreAssetPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Neutral"]          = "Stores/store-neutral.json",
            ["Number One"]       = "Stores/store-number-one.json",
            ["Jade Temu"]        = "Stores/store-jade-temu.json",
            ["Arachne Req"]      = "Stores/store-arachne-req.json",
            ["Salaam Suuk"]      = "Stores/store-salaam-suuk.json",
            ["Frontier General"] = "Stores/store-frontier-general.json",
            ["Alpha Sec"]        = "Stores/store-alpha-sec.json",
            ["Greengrocer"]      = "Stores/store-greengrocer.json",
            ["Bantai Yamaco"]    = "Stores/store-bantai-yamaco.json",
            ["Exrah Surplus"]    = "Stores/store-exrah-surplus.json"
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Dictionary<string, Store> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAllStoreNames() => StoreAssetPaths.Keys.ToList();

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(string Name, string? AssociatedType, string Alignment)>> GetAvailableStoresAsync(
        IReadOnlyList<string> factionNames,
        CancellationToken cancellationToken = default)
    {
        var nameSet = new HashSet<string>(factionNames, StringComparer.OrdinalIgnoreCase);

        var results = new List<(string Name, string? AssociatedType, string Alignment)>();
        foreach (var storeName in StoreAssetPaths.Keys)
        {
            var store = await LoadStoreAsync(storeName, cancellationToken).ConfigureAwait(false);
            if (store is null)
                continue;

            var isNeutral = store.AssociatedFactions.Count == 0;
            var matchesFaction = store.AssociatedFactions.Any(nameSet.Contains);

            if (isNeutral || matchesFaction)
                results.Add((store.Name, store.AssociatedType, store.Alignment));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Store?> GetStoreByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await LoadStoreAsync(name, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Store?> LoadStoreAsync(string name, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(name, out var cached))
                return cached;

            if (!StoreAssetPaths.TryGetValue(name, out var assetPath))
                return null;

            await using var stream = await FileSystem.Current
                .OpenAppPackageFileAsync(assetPath)
                .ConfigureAwait(false);

            var store = await JsonSerializer
                .DeserializeAsync<Store>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (store is not null)
                _cache[name] = store;

            return store;
        }
        finally
        {
            _lock.Release();
        }
    }
}
