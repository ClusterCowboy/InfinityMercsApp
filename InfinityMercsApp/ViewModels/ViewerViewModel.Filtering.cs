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
    public bool MercsOnlyUnits
    {
        get => _mercsOnlyUnits;
        set
        {
            if (_mercsOnlyUnits == value)
            {
                return;
            }

            _mercsOnlyUnits = value;
            OnPropertyChanged();

            if (SelectedFaction is not null)
            {
                _ = LoadUnitsForSelectedFactionAsync();
            }
        }
    }

    public bool LieutenantOnlyUnits
    {
        get => _lieutenantOnlyUnits;
        set
        {
            if (_lieutenantOnlyUnits == value)
            {
                return;
            }

            _lieutenantOnlyUnits = value;
            OnPropertyChanged();

            if (SelectedFaction is not null)
            {
                _ = LoadUnitsForSelectedFactionAsync();
            }
        }
    }

    public UnitFilterCriteria ActiveUnitFilter => _activeUnitFilter;

    public async Task ApplyActiveUnitFilterAsync(UnitFilterCriteria? criteria, CancellationToken cancellationToken = default)
    {
        _activeUnitFilter = criteria ?? UnitFilterCriteria.None;
        LieutenantOnlyUnits = _activeUnitFilter.LieutenantOnlyUnits;

        if (SelectedFaction is null)
        {
            return;
        }

        await LoadUnitsForSelectedFactionAsync(cancellationToken);
    }

    public Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPoints = 200;

        if (SelectedFaction is null || _armyDataService is null)
        {
            return Task.FromResult(new UnitFilterPopupOptions
            {
                Classification = [],
                Characteristics = [],
                Skills = [],
                Equipment = [],
                Weapons = [],
                Ammo = [],
                MinPoints = 0,
                MaxPoints = maxPoints
            });
        }

        var units = _armyDataService.GetResumeByFaction(SelectedFaction.Id, cancellationToken);
        var snapshot = _armyDataService.GetFactionSnapshot(SelectedFaction.Id, cancellationToken);
        var filtersJson = snapshot?.FiltersJson;
        var typeLookup = BuildIdNameLookup(filtersJson, "type");
        var charsLookup = BuildIdNameLookup(filtersJson, "chars");
        var skillsLookup = BuildIdNameLookup(filtersJson, "skills");
        var equipLookup = BuildIdNameLookup(filtersJson, "equip");
        var weaponsLookup = BuildIdNameLookup(filtersJson, "weapons");
        var ammoLookup = BuildIdNameLookup(filtersJson, "ammunition");

        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var typeName) && !string.IsNullOrWhiteSpace(typeName))
            {
                classification.Add(typeName.Trim());
            }

            var unitRecord = _armyDataService.GetUnit(SelectedFaction.Id, unit.UnitId, cancellationToken);
            if (string.IsNullOrWhiteSpace(unitRecord?.ProfileGroupsJson))
            {
                continue;
            }

            AddFilterOptionsFromProfilesAndOptions(
                unitRecord.ProfileGroupsJson,
                charsLookup,
                skillsLookup,
                equipLookup,
                weaponsLookup,
                ammoLookup,
                characteristics,
                skills,
                equipment,
                weapons,
                ammo,
                ref maxPoints);
        }

        return Task.FromResult(new UnitFilterPopupOptions
        {
            Classification = classification.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Characteristics = characteristics.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Skills = skills.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Equipment = equipment.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Weapons = weapons.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Ammo = ammo.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            MinPoints = 0,
            MaxPoints = Math.Max(1, maxPoints)
        });
    }

    public bool ShowAllFactionEntries
    {
        get => _factionFilterMode == FactionFilterMode.All;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.All)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.All;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowFactionEntriesOnly));
            OnPropertyChanged(nameof(ShowSectorialEntriesOnly));
            ApplyFactionFilter();
        }
    }

    public bool ShowFactionEntriesOnly
    {
        get => _factionFilterMode == FactionFilterMode.Factions;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.Factions)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.Factions;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowAllFactionEntries));
            OnPropertyChanged(nameof(ShowSectorialEntriesOnly));
            ApplyFactionFilter();
        }
    }

    public bool ShowSectorialEntriesOnly
    {
        get => _factionFilterMode == FactionFilterMode.Sectorials;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.Sectorials)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.Sectorials;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowAllFactionEntries));
            OnPropertyChanged(nameof(ShowFactionEntriesOnly));
            ApplyFactionFilter();
        }
    }

}
