using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    public async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        if (_armyDataService is null)
        {
            Status = "Metadata service unavailable.";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = _armyDataService.GetMetadataFactions(includeDiscontinued: true, cancellationToken);
            if (_factionLogoCacheService is not null)
            {
                var factionRecords = factions.Select(x => new FactionRecord
                {
                    Id = x.Id,
                    ParentId = x.ParentId,
                    Name = x.Name,
                    Slug = x.Slug,
                    Discontinued = x.Discontinued,
                    Logo = x.Logo
                });
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factionRecords, cancellationToken);
            }

            _allFactions = factions.Select(faction => new ViewerFactionItem
            {
                Id = faction.Id,
                ParentId = faction.ParentId,
                Name = faction.Name,
                Logo = faction.Logo,
                CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                PackagedLogoPath = _factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id)
                    ?? $"SVGCache/factions/{faction.Id}.svg"
            }).ToList();

            ApplyFactionFilter();

            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadFactionsAsync failed: {ex.Message}");
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadUnitsForSelectedFactionAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        SelectedUnit = null;
        ResetUnitDetails();
        ResetFireteamCounts();

        if (SelectedFaction is null)
        {
            UnitsStatus = "Select a faction.";
            return;
        }

        if (_armyDataService is null)
        {
            UnitsStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            UnitsStatus = "Loading units...";
            var units = _armyDataService.GetResumeByFaction(SelectedFaction.Id, cancellationToken);

            var snapshot = _armyDataService.GetFactionSnapshot(SelectedFaction.Id, cancellationToken);
            UpdateFireteamCounts(snapshot?.FireteamChartJson);
            var allowedFireteamSlugs = units
                .Select(x => x.Slug?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allowedFireteamNames = units
                .Select(x => x.Name?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => NormalizeFireteamUnitName(x!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            UpdateFireteamTeams(
                snapshot?.FireteamChartJson,
                MercsOnlyUnits,
                allowedFireteamSlugs,
                allowedFireteamNames);
            var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var weaponsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            var ammoLookup = BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");

            if (_factionLogoCacheService is not null)
            {
                UnitsStatus = "Preparing unit SVG cache...";
                var cacheResult = await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(
                    SelectedFaction.Id,
                    units,
                    cancellationToken);
                Console.Error.WriteLine($"Unit cache for faction {SelectedFaction.Id}: downloaded={cacheResult.Downloaded}, reused={cacheResult.CachedReuse}, failed={cacheResult.Failed}");
            }

            var orderedUnits = units
                .OrderBy(unit => ArmyUnitSort.GetUnitTypeSortIndex(unit.Type))
                .ThenBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var unit in orderedUnits)
            {
                var unitRecord = _armyDataService.GetUnit(SelectedFaction.Id, unit.UnitId, cancellationToken);

                if (!MatchesClassificationFilter(unit, typeLookup, _activeUnitFilter))
                {
                    continue;
                }

                if (!UnitHasVisibleOptionWithFilter(
                        unitRecord?.ProfileGroupsJson,
                        skillsLookup,
                        charsLookup,
                        equipLookup,
                        weaponsLookup,
                        ammoLookup,
                        _activeUnitFilter,
                        requireLieutenant: _activeUnitFilter.LieutenantOnlyUnits,
                        requireZeroSwc: false))
                {
                    continue;
                }

                Units.Add(new ViewerUnitItem
                {
                    Id = unit.UnitId,
                    Name = unit.Name,
                    Logo = unit.Logo,
                    Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(SelectedFaction.Id, unit.UnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(SelectedFaction.Id, unit.UnitId)
                        ?? $"SVGCache/units/{SelectedFaction.Id}-{unit.UnitId}.svg"
                });
            }

            UnitsStatus = Units.Count == 0 ? "No units available for this faction." : $"{Units.Count} units loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadUnitsForSelectedFactionAsync failed: {ex.Message}");
            UnitsStatus = $"Failed to load units: {ex.Message}";
        }
    }

}
