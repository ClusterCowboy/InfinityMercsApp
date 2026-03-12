using InfinityMercsApp.Domain.CompanyCreation;

namespace InfinityMercsApp.Infrastructure.Services;

/// <inheritdoc/>
public class ArmySourceSelectionModeService : IArmySourceSelectionModeService
{
    private ArmySourceSelectionMode _mode;

    /// <inheritdoc/>
    public ArmySourceSelectionMode Get()
    {
        return _mode;
    }

    /// <inheritdoc/>
    public void Set(ArmySourceSelectionMode mode)
    {
        _mode = mode;
    }
}
