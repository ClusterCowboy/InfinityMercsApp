namespace InfinityMercsApp.Data.Database;

public interface IMetadataAccessor
{
    Task ImportFromJsonAsync(string json, CancellationToken cancellationToken = default);

    Task ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<bool> HasMetadataAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FactionRecord>> GetFactionsAsync(bool includeDiscontinued = false, CancellationToken cancellationToken = default);

    Task<FactionRecord?> GetFactionByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeaponRecord>> SearchWeaponsByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillRecord>> GetSkillsAsync(CancellationToken cancellationToken = default);
}
