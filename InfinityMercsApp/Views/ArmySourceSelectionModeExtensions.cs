namespace InfinityMercsApp.Views;

public static class ArmySourceSelectionModeExtensions
{
    public static bool IsTagCompanyMode(this ArmySourceSelectionMode mode)
    {
        return mode is ArmySourceSelectionMode.TagVanillaFactions
            or ArmySourceSelectionMode.TagSectorials
            or ArmySourceSelectionMode.TagSingleSource;
    }

    public static bool IsVanillaFactionMode(this ArmySourceSelectionMode mode)
    {
        return mode is ArmySourceSelectionMode.VanillaFactions
            or ArmySourceSelectionMode.TagVanillaFactions;
    }

    public static bool IsTagSingleSourceMode(this ArmySourceSelectionMode mode)
    {
        return mode == ArmySourceSelectionMode.TagSingleSource;
    }

    public static bool ShowsRightSelectionBox(this ArmySourceSelectionMode mode)
    {
        return mode is ArmySourceSelectionMode.Sectorials
            or ArmySourceSelectionMode.TagSectorials;
    }

    public static string GetFactionSelectionHeading(this ArmySourceSelectionMode mode)
    {
        if (mode.IsTagSingleSourceMode())
        {
            return "Choose your faction or sectorial:";
        }

        return mode.IsVanillaFactionMode()
            ? "Choose your faction:"
            : "Choose your sectorials";
    }

    public static string GetCompanyTypeLabel(this ArmySourceSelectionMode mode)
    {
        return mode switch
        {
            ArmySourceSelectionMode.VanillaFactions => "Standard Company - Vanilla",
            ArmySourceSelectionMode.Sectorials => "Standard Company - Sectorial",
            ArmySourceSelectionMode.TagSingleSource => "TAG Company",
            ArmySourceSelectionMode.TagVanillaFactions => "TAG Company - Vanilla",
            ArmySourceSelectionMode.TagSectorials => "TAG Company - Sectorial",
            _ => "Unknown Company Type"
        };
    }
}
