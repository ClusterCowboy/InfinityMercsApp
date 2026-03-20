using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionPageInteractionWorkflow
{
    internal static Command CreateSelectFactionCommand<TFaction>(Action<TFaction> setSelectedFaction)
        where TFaction : class
    {
        return new Command<TFaction>(item =>
        {
            if (item is null)
            {
                return;
            }

            setSelectedFaction(item);
        });
    }

    internal static Command CreateSelectUnitCommand<TUnit>(
        Action<TUnit> setSelectedUnit,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId,
        Func<TUnit, string?> readUnitName)
        where TUnit : class
    {
        return new Command<TUnit>(item =>
        {
            if (item is null)
            {
                Console.Error.WriteLine("CompanySelectionPage SelectUnitCommand invoked with null item.");
                return;
            }

            Console.WriteLine(
                $"CompanySelectionPage SelectUnitCommand: id={readUnitId(item)}, faction={readSourceFactionId(item)}, name='{readUnitName(item)}'.");
            setSelectedUnit(item);
        });
    }

    internal static Command CreateStartCompanyCommand(Func<Task> startCompanyAsync, Func<bool> canExecute)
    {
        return new Command(async () => await startCompanyAsync(), canExecute);
    }

    internal static void WireFactionSlotTapHandlers(
        FactionSlotSelectorView factionSlotSelectorView,
        Action<int> setActiveSlot,
        Func<bool> showRightSelectionBox)
    {
        factionSlotSelectorView.LeftSlotTapped += (_, _) => setActiveSlot(0);
        factionSlotSelectorView.RightSlotTapped += (_, _) =>
        {
            if (showRightSelectionBox())
            {
                setActiveSlot(1);
            }
        };
    }

    internal static void FinalizePageInitialization(
        Action assignBindingContext,
        Action setInitialActiveSlot,
        Action refreshSummaryFormatted,
        Func<Task> loadHeaderIconsAsync)
    {
        assignBindingContext();
        setInitialActiveSlot();
        refreshSummaryFormatted();
        _ = loadHeaderIconsAsync();
    }

    internal static void HandleTeamAllowedProfileSelected<TTeamItem, TUnit>(
        TTeamItem? teamItem,
        IEnumerable<TUnit> units,
        Action<TUnit, bool> applySelection,
        Func<TTeamItem, bool>? shouldRestrictProfiles = null)
        where TTeamItem : CompanyTeamUnitLimitItemBase
        where TUnit : CompanyUnitSelectionItemBase
    {
        if (teamItem is null)
        {
            Console.Error.WriteLine("CompanySelectionPage OnTeamAllowedProfileTappedFromView: no team item binding context.");
            return;
        }

        var unitList = units as IReadOnlyList<TUnit> ?? units.ToList();
        var resolved = CompanyTeamSelectionWorkflow.ResolveSelectedTeamUnit<TUnit>(
            unitList,
            teamItem.ResolvedUnitId,
            teamItem.ResolvedSourceFactionId,
            teamItem.Slug,
            teamItem.Name,
            x => x.Id,
            x => x.SourceFactionId,
            x => x.Slug,
            x => x.Name);

        if (resolved is null)
        {
            Console.Error.WriteLine(
                $"CompanySelectionPage OnTeamAllowedProfileTappedFromView: unable to resolve unit for team entry '{teamItem.Name}'.");
            return;
        }

        var restrictProfiles = shouldRestrictProfiles?.Invoke(teamItem) ?? false;
        applySelection(resolved, restrictProfiles);
    }

    internal static void WireUnitDisplayEvents(
        UnitDisplayConfigurationsView unitDisplayView,
        EventHandler<SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs> onHeaderIconsPaintSurface,
        EventHandler<SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs> onSelectedUnitPaintSurface,
        EventHandler<SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs> onPeripheralIconPaintSurface,
        EventHandler<SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs> onProfileTacticalPaintSurface,
        EventHandler<EventArgs> onUnitNameHeadingSizeChanged)
    {
        unitDisplayView.HeaderIconsCanvasPaintSurface += onHeaderIconsPaintSurface;
        unitDisplayView.SelectedUnitCanvasPaintSurface += onSelectedUnitPaintSurface;
        unitDisplayView.PeripheralIconCanvasPaintSurface += onPeripheralIconPaintSurface;
        unitDisplayView.ProfileTacticalIconCanvasPaintSurface += onProfileTacticalPaintSurface;
        unitDisplayView.UnitNameHeadingSizeChanged += onUnitNameHeadingSizeChanged;
    }
}
