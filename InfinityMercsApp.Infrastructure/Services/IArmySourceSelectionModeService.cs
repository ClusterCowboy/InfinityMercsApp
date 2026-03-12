using InfinityMercsApp.Domain.CompanyCreation;

namespace InfinityMercsApp.Infrastructure.Services;

/// <summary>
/// An interface that maintains an in-memory representation of the army selection mode.
/// Obviously a kludge for now and will be replaced, but exists for now
/// So that dependency injection and routing can be used properly on pages
/// That currently depend on ArmySelectionMode
/// </summary>
public interface IArmySourceSelectionModeService
{
    /// <summary>
    /// Gets the army selection mode.
    /// </summary>
    /// <returns></returns>
    ArmySourceSelectionMode Get();

    /// <summary>
    /// Sets the army selection mode.
    /// </summary>
    /// <param name="mode"></param>
    void Set(ArmySourceSelectionMode mode);
}
