namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Generates the synthetic "Inspiring Company" faction (ID 2002) by scanning all stored factions
/// for units with the Inspiring Leadership skill, then writing the result to the database.
/// </summary>
public interface IInspiringCompanyFactionGenerator
{
    /// <summary>
    /// Generates or regenerates the synthetic Inspiring Company faction from all stored faction data.
    /// Skips the write if the generated content matches the previously stored version.
    /// </summary>
    Task GenerateAsync(CancellationToken cancellationToken = default);
}
