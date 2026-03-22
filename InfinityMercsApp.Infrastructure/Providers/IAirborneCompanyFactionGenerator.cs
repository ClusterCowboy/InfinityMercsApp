namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Generates the synthetic "Airborne Company" faction (ID 2001) by scanning all stored factions
/// for units with the Airborne Deployment skill, then writing the result to the database.
/// </summary>
public interface IAirborneCompanyFactionGenerator
{
    /// <summary>
    /// Generates or regenerates the synthetic Airborne Company faction from all stored faction data.
    /// Skips the write if the generated content matches the previously stored version.
    /// </summary>
    Task GenerateAsync(CancellationToken cancellationToken = default);
}
