namespace InfinityMercsApp.Data.WebAccess;

public interface IWebAccessObject
{
    Task<string> GetMetaDataAsync(CancellationToken cancellationToken = default);
    Task<string> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default);
}
