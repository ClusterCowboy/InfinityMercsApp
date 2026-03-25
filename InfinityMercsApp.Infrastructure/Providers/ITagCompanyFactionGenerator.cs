namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Ensures the synthetic "TAG Company" faction (ID 2003) exists in the database.
/// The faction is created with metadata and an empty army payload until units are added later.
/// </summary>
public interface ITagCompanyFactionGenerator
{
    /// <summary>
    /// Creates or refreshes the synthetic TAG Company faction.
    /// </summary>
    Task GenerateAsync(CancellationToken cancellationToken = default);
}
