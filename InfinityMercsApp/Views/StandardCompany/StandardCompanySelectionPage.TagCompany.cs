using System.Globalization;
using System.Linq;
using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;
using PopupCaptainInputBuilder = InfinityMercsApp.Views.Common.Captain.CaptainPopupInputBuilder;
using PopupCaptainOptionSet = InfinityMercsApp.Views.Common.Captain.CaptainUpgradeOptionSet;
using PopupCaptainPopupContext = InfinityMercsApp.Views.Common.Captain.CaptainUpgradePopupContext;
using PopupCaptainPopupPage = InfinityMercsApp.Views.Common.Captain.ConfigureCaptainPopupPage;
using PopupCaptainUnitInfo = InfinityMercsApp.Views.Common.Captain.CaptainUnitPopupInfo;
using PopupSavedImprovedCaptainStats = InfinityMercsApp.Views.Common.Captain.SavedImprovedCaptainStats;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage
{
    private readonly TagCompanyCustomTagModel _tagCompanyCustomTagModel = TagCompanyCustomTagModel.Default;
    private string _tagCompanyCustomTagNameInput = TagCompanyCustomTagModel.Default.ResolveName(null);

    public bool ShowTagCompanyCustomTagControls => _mode.IsTagCompanyMode();

    public string TagCompanyCustomTagNameInput
    {
        get => _tagCompanyCustomTagNameInput;
        set
        {
            if (string.Equals(_tagCompanyCustomTagNameInput, value, StringComparison.Ordinal))
            {
                return;
            }

            SetTagCompanyCustomTagName(value);
        }
    }

    private void EnsureTagCompanyCustomTagEntry()
    {
        if (!ShowTagCompanyCustomTagControls || GetTagCompanyCustomTagEntry() is not null)
        {
            SyncTagCompanyCustomTagNameInput();
            return;
        }

        MercsCompanyEntries.Add(BuildTagCompanyCustomTagEntry());
        SyncTagCompanyCustomTagNameInput();
    }

    private MercsCompanyEntry BuildTagCompanyCustomTagEntry(
        string? customName = null,
        PopupSavedImprovedCaptainStats? specopsStats = null,
        int? sourceFactionId = null,
        bool isLieutenant = false)
    {
        var configuredTag = _tagCompanyCustomTagModel.BuildProfile(
            customName: customName,
            extraWeapons: [specopsStats?.WeaponChoice1, specopsStats?.WeaponChoice2, specopsStats?.WeaponChoice3],
            extraSkills: [specopsStats?.SkillChoice1, specopsStats?.SkillChoice2, specopsStats?.SkillChoice3],
            extraEquipment: [specopsStats?.EquipmentChoice1, specopsStats?.EquipmentChoice2, specopsStats?.EquipmentChoice3],
            ccBonus: specopsStats?.CcBonus ?? 0,
            bsBonus: specopsStats?.BsBonus ?? 0,
            phBonus: specopsStats?.PhBonus ?? 0,
            wipBonus: specopsStats?.WipBonus ?? 0,
            armBonus: specopsStats?.ArmBonus ?? 0,
            btsBonus: specopsStats?.BtsBonus ?? 0,
            vitalityBonus: specopsStats?.VitalityBonus ?? 0,
            spentExperience: specopsStats?.SpentExperience ?? 0);

        var resolvedSourceFactionId = sourceFactionId ?? ResolveTagCompanySpecopsSourceFactionId();
        if (resolvedSourceFactionId <= 0)
        {
            resolvedSourceFactionId = TagCompanyCustomTagModel.DefaultSourceFactionId;
        }

        var moveDisplay = FormatMoveValue(_tagCompanyCustomTagModel.MoveFirstCm, _tagCompanyCustomTagModel.MoveSecondCm);

        return new MercsCompanyEntry
        {
            Name = configuredTag.Name,
            BaseUnitName = _tagCompanyCustomTagModel.DefaultName,
            NameFormatted = CompanyProfileTextService.BuildNameFormatted(configuredTag.Name),
            Subtitle = configuredTag.Statline,
            UnitTypeCode = _tagCompanyCustomTagModel.UnitTypeCode,
            CostDisplay = $"C {configuredTag.Cost.ToString(CultureInfo.InvariantCulture)}",
            CostValue = configuredTag.Cost,
            ProfileKey = _tagCompanyCustomTagModel.ProfileKey,
            IsLieutenant = isLieutenant,
            SourceUnitId = TagCompanyCustomTagModel.DefaultSourceUnitId,
            SourceFactionId = resolvedSourceFactionId,
            PackagedLogoPath = _tagCompanyCustomTagModel.ResolvePackagedLogoPath(null),
            SavedEquipment = configuredTag.Equipment,
            SavedSkills = configuredTag.Skills,
            SavedRangedWeapons = configuredTag.RangedWeapons,
            SavedCcWeapons = configuredTag.CcWeapons,
            CanRemove = false,
            UnitMoveFirstCm = _tagCompanyCustomTagModel.MoveFirstCm,
            UnitMoveSecondCm = _tagCompanyCustomTagModel.MoveSecondCm,
            UnitMoveDisplay = moveDisplay,
            ExperiencePoints = configuredTag.ExperiencePoints,
            EquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", configuredTag.Equipment, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = configuredTag.HasEquipmentLine,
            SkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", configuredTag.Skills, Color.FromArgb("#F59E0B")),
            HasSkillsLine = !string.Equals(configuredTag.Skills, "-", StringComparison.Ordinal),
            RangedLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Ranged Weapons", configuredTag.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("CC Weapons", configuredTag.CcWeapons, Color.FromArgb("#22C55E"))
        };
    }

    private int ResolveTagCompanySpecopsSourceFactionId()
    {
        var sourceFaction = CompanyUnitDetailsShared.BuildUnitSourceFactions(
                ShowRightSelectionBox,
                _factionSelectionState.LeftSlotFaction,
                _factionSelectionState.RightSlotFaction,
                faction => faction.Id)
            .FirstOrDefault();

        return sourceFaction?.Id ?? TagCompanyCustomTagModel.DefaultSourceFactionId;
    }

    private SavedImprovedCaptainStats? TryBuildPreconfiguredCaptainStats(MercsCompanyEntry entry)
    {
        if (!entry.IsLieutenant || !_tagCompanyCustomTagModel.IsCustomTagProfile(entry.ProfileKey))
        {
            return null;
        }

        var captainName = string.IsNullOrWhiteSpace(entry.Name)
            ? _tagCompanyCustomTagModel.ResolveName(TagCompanyCustomTagNameInput)
            : entry.Name.Trim();

        return new SavedImprovedCaptainStats
        {
            IsEnabled = false,
            IsLieutenant = true,
            CaptainName = captainName,
            OptionFactionId = entry.SourceFactionId,
            OptionFactionName = _armyDataService.GetMetadataFactionById(entry.SourceFactionId)?.Name ?? string.Empty,
            SpentExperience = Math.Max(0, entry.ExperiencePoints)
        };
    }

    private MercsCompanyEntry? GetTagCompanyCustomTagEntry()
    {
        return MercsCompanyEntries.FirstOrDefault(entry => _tagCompanyCustomTagModel.IsCustomTagProfile(entry.ProfileKey));
    }

    private void SyncTagCompanyCustomTagNameInput()
    {
        var resolvedName = GetTagCompanyCustomTagEntry()?.Name ?? _tagCompanyCustomTagModel.ResolveName(_tagCompanyCustomTagNameInput);
        if (string.Equals(_tagCompanyCustomTagNameInput, resolvedName, StringComparison.Ordinal))
        {
            return;
        }

        _tagCompanyCustomTagNameInput = resolvedName;
        OnPropertyChanged(nameof(TagCompanyCustomTagNameInput));
    }

    private void SetTagCompanyCustomTagName(string? value)
    {
        var normalized = _tagCompanyCustomTagModel.ResolveName(value);
        if (!string.Equals(_tagCompanyCustomTagNameInput, normalized, StringComparison.Ordinal))
        {
            _tagCompanyCustomTagNameInput = normalized;
            OnPropertyChanged(nameof(TagCompanyCustomTagNameInput));
        }

        var entry = GetTagCompanyCustomTagEntry();
        if (entry is null || string.Equals(entry.Name, normalized, StringComparison.Ordinal))
        {
            if (_selectedUnit is null && ShowTagCompanyCustomTagControls)
            {
                UnitNameHeading = normalized;
            }

            return;
        }

        var replacementEntry = CloneMercsCompanyEntry(entry, normalized, entry.IsLieutenant);
        ReplaceMercsCompanyEntry(entry, replacementEntry);

        if (_selectedUnit is null && ShowTagCompanyCustomTagControls)
        {
            UnitNameHeading = normalized;
        }
    }

    private MercsCompanyEntry CloneMercsCompanyEntry(MercsCompanyEntry source, string name, bool isLieutenant)
    {
        var clone = new MercsCompanyEntry
        {
            Name = name,
            BaseUnitName = source.BaseUnitName,
            NameFormatted = CompanyProfileTextService.BuildNameFormatted(name),
            CostDisplay = source.CostDisplay,
            CostValue = source.CostValue,
            ProfileKey = source.ProfileKey,
            IsLieutenant = isLieutenant,
            SourceUnitId = source.SourceUnitId,
            SourceFactionId = source.SourceFactionId,
            CachedLogoPath = source.CachedLogoPath,
            PackagedLogoPath = source.PackagedLogoPath,
            Subtitle = source.Subtitle,
            UnitTypeCode = source.UnitTypeCode,
            SavedEquipment = source.SavedEquipment,
            SavedSkills = source.SavedSkills,
            SavedRangedWeapons = source.SavedRangedWeapons,
            SavedCcWeapons = source.SavedCcWeapons,
            CanRemove = source.CanRemove,
            UnitMoveFirstCm = source.UnitMoveFirstCm,
            UnitMoveSecondCm = source.UnitMoveSecondCm,
            UnitMoveDisplay = source.UnitMoveDisplay,
            HasPeripheralStatBlock = source.HasPeripheralStatBlock,
            PeripheralNameHeading = source.PeripheralNameHeading,
            PeripheralMov = source.PeripheralMov,
            PeripheralCc = source.PeripheralCc,
            PeripheralBs = source.PeripheralBs,
            PeripheralPh = source.PeripheralPh,
            PeripheralWip = source.PeripheralWip,
            PeripheralArm = source.PeripheralArm,
            PeripheralBts = source.PeripheralBts,
            PeripheralVitalityHeader = source.PeripheralVitalityHeader,
            PeripheralVitality = source.PeripheralVitality,
            PeripheralS = source.PeripheralS,
            PeripheralAva = source.PeripheralAva,
            PeripheralMoveFirstCm = source.PeripheralMoveFirstCm,
            PeripheralMoveSecondCm = source.PeripheralMoveSecondCm,
            SavedPeripheralEquipment = source.SavedPeripheralEquipment,
            SavedPeripheralSkills = source.SavedPeripheralSkills,
            EquipmentLineFormatted = source.EquipmentLineFormatted,
            HasEquipmentLine = source.HasEquipmentLine,
            SkillsLineFormatted = source.SkillsLineFormatted,
            HasSkillsLine = source.HasSkillsLine,
            RangedLineFormatted = source.RangedLineFormatted,
            CcLineFormatted = source.CcLineFormatted,
            PeripheralEquipmentLineFormatted = source.PeripheralEquipmentLineFormatted,
            HasPeripheralEquipmentLine = source.HasPeripheralEquipmentLine,
            PeripheralSkillsLineFormatted = source.PeripheralSkillsLineFormatted,
            HasPeripheralSkillsLine = source.HasPeripheralSkillsLine,
            ExperiencePoints = source.ExperiencePoints
        };

        clone.IsSelected = source.IsSelected;
        clone.IsIrregular = source.IsIrregular;
        clone.NormallyIrregular = source.NormallyIrregular;
        return clone;
    }

    private void ReplaceMercsCompanyEntry(MercsCompanyEntry existingEntry, MercsCompanyEntry replacementEntry)
    {
        var entryIndex = MercsCompanyEntries.IndexOf(existingEntry);
        if (entryIndex < 0)
        {
            return;
        }

        MercsCompanyEntries[entryIndex] = replacementEntry;
    }

    private async Task ConfigureTagCompanyCustomTagAsync(CancellationToken cancellationToken = default)
    {
        if (!ShowTagCompanyCustomTagControls)
        {
            return;
        }

        EnsureTagCompanyCustomTagEntry();
        var entry = GetTagCompanyCustomTagEntry();
        if (entry is null)
        {
            return;
        }

        var sourceFactionId = ResolveTagCompanySpecopsSourceFactionId();
        if (sourceFactionId <= 0 || sourceFactionId == TagCompanyCustomTagModel.DefaultSourceFactionId)
        {
            await DisplayAlert("TAG Configuration", "Select a faction or sectorial before configuring the Custom TAG.", "OK");
            return;
        }

        var optionFactionId = PopupCaptainInputBuilder.ResolveOptionFactionId(
            sourceFactionId,
            Factions.FirstOrDefault(faction => faction.Id == sourceFactionId)?.ParentId);
        var options = await PopupCaptainInputBuilder.LoadUpgradeOptionsAsync(
            _armyDataService,
            _specOpsProvider,
            optionFactionId,
            ShowUnitsInInches,
            cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = await PopupCaptainInputBuilder.LoadUpgradeOptionsAsync(
                _armyDataService,
                _specOpsProvider,
                sourceFactionId,
                ShowUnitsInInches,
                cancellationToken);
            optionFactionId = sourceFactionId;
        }

        options = ApplyTagSpecOpsRestrictions(options);

        var hasCharacterLieutenant = MercsCompanyEntries.Any(otherEntry =>
            !ReferenceEquals(otherEntry, entry) &&
            otherEntry.IsLieutenant &&
            !_tagCompanyCustomTagModel.IsCustomTagProfile(otherEntry.ProfileKey));

        var context = new PopupCaptainPopupContext
        {
            Unit = new PopupCaptainUnitInfo
            {
                Name = entry.Name,
                Cost = entry.CostValue,
                Statline = _tagCompanyCustomTagModel.ResolveSavedStatline(entry.Subtitle),
                RangedWeapons = entry.SavedRangedWeapons,
                CcWeapons = entry.SavedCcWeapons,
                Skills = entry.SavedSkills,
                Equipment = entry.SavedEquipment,
                CachedLogoPath = entry.CachedLogoPath,
                PackagedLogoPath = _tagCompanyCustomTagModel.ResolvePackagedLogoPath(entry.PackagedLogoPath)
            },
            OptionFactionId = optionFactionId,
            OptionFactionName = ResolveTagCompanyOptionFactionName(sourceFactionId, optionFactionId),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment,
            PopupTitle = "Custom TAG Configuration",
            ConfirmButtonText = "Apply TAG Upgrades",
            CancelButtonText = "Cancel",
            ExperienceBudget = TagCompanyCustomTagModel.SpecOpsExperienceBudget,
            ExperienceLabel = "Spec Ops XP Remaining",
            InitialName = entry.Name,
            NamePlaceholder = _tagCompanyCustomTagModel.DefaultName,
            AllowNameEdit = false,
            AllowArmUpgrade = false,
            AllowBtsUpgrade = false,
            AllowVitalityUpgrade = false,
            ShowLieutenantOption = !hasCharacterLieutenant,
            InitialIsLieutenant = !hasCharacterLieutenant && entry.IsLieutenant,
            LieutenantOptionLabel = "Lieutenant"
        };

        var configuredStats = await PopupCaptainPopupPage.ShowAsync(Navigation, context);
        if (configuredStats is null)
        {
            return;
        }

        var replacementEntry = BuildTagCompanyCustomTagEntry(
            customName: entry.Name,
            specopsStats: configuredStats,
            sourceFactionId: sourceFactionId,
            isLieutenant: configuredStats.IsLieutenant);
        replacementEntry.IsSelected = entry.IsSelected;

        if (replacementEntry.IsLieutenant)
        {
            for (var index = 0; index < MercsCompanyEntries.Count; index++)
            {
                var otherEntry = MercsCompanyEntries[index];
                if (ReferenceEquals(otherEntry, entry) || !otherEntry.IsLieutenant)
                {
                    continue;
                }

                MercsCompanyEntries[index] = CloneMercsCompanyEntry(otherEntry, otherEntry.Name, isLieutenant: false);
            }
        }

        ReplaceMercsCompanyEntry(entry, replacementEntry);
        SyncTagCompanyCustomTagNameInput();
        await ShowTagCompanyCustomTagDetailsAsync(replacementEntry);
        UpdateMercsCompanyTotal();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync(cancellationToken);
    }

    private PopupCaptainOptionSet ApplyTagSpecOpsRestrictions(PopupCaptainOptionSet optionSet)
    {
        if (optionSet.IsEmpty)
        {
            return optionSet;
        }

        return new PopupCaptainOptionSet
        {
            Weapons = optionSet.Weapons
                .Where(option => !IsTagRestrictedSpecOpsChoice(option))
                .ToList(),
            Skills = optionSet.Skills
                .Where(option => !IsTagRestrictedSpecOpsChoice(option))
                .ToList(),
            Equipment = optionSet.Equipment
                .Where(option => !IsTagRestrictedSpecOpsChoice(option))
                .ToList()
        };
    }

    private bool IsTagRestrictedSpecOpsChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TagCompanyCustomTagModel.RestrictedSpecOpsKeywords.Any(keyword =>
            value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveTagCompanyOptionFactionName(int sourceFactionId, int optionFactionId)
    {
        var sourceFactionName = Factions.FirstOrDefault(faction => faction.Id == sourceFactionId)?.Name;
        var optionFactionName = Factions.FirstOrDefault(faction => faction.Id == optionFactionId)?.Name;
        var metadataSourceFactionName = sourceFactionId > 0
            ? _armyDataService.GetMetadataFactionById(sourceFactionId)?.Name
            : null;
        var metadataOptionFactionName = optionFactionId > 0
            ? _armyDataService.GetMetadataFactionById(optionFactionId)?.Name
            : null;

        return PopupCaptainInputBuilder.ResolveOptionFactionName(
            sourceFactionId,
            optionFactionId,
            sourceFactionName,
            optionFactionName,
            metadataSourceFactionName,
            metadataOptionFactionName);
    }

    private async Task ShowTagCompanyCustomTagDetailsAsync(MercsCompanyEntry entry)
    {
        foreach (var unit in Units)
        {
            unit.IsSelected = false;
        }

        _selectedUnit = null;
        ResetUnitDetails(clearLogo: false);
        SyncTagCompanyCustomTagNameInput();
        UnitNameHeading = entry.Name;
        var statline = UnitStatline.ParseSegments(_tagCompanyCustomTagModel.ResolveSavedStatline(entry.Subtitle));
        var vitalityHeader = UnitStatline.ResolveVitalityHeader(statline, _tagCompanyCustomTagModel.VitalityHeader);

        UnitMoveFirstCm = _tagCompanyCustomTagModel.MoveFirstCm;
        UnitMoveSecondCm = _tagCompanyCustomTagModel.MoveSecondCm;
        UnitMov = FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
        UnitCc = UnitStatline.ReadValue(statline, "CC", _tagCompanyCustomTagModel.Cc.ToString(CultureInfo.InvariantCulture));
        UnitBs = UnitStatline.ReadValue(statline, "BS", _tagCompanyCustomTagModel.Bs.ToString(CultureInfo.InvariantCulture));
        UnitPh = UnitStatline.ReadValue(statline, "PH", _tagCompanyCustomTagModel.Ph.ToString(CultureInfo.InvariantCulture));
        UnitWip = UnitStatline.ReadValue(statline, "WIP", _tagCompanyCustomTagModel.Wip.ToString(CultureInfo.InvariantCulture));
        UnitArm = UnitStatline.ReadValue(statline, "ARM", _tagCompanyCustomTagModel.Arm.ToString(CultureInfo.InvariantCulture));
        UnitBts = UnitStatline.ReadValue(statline, "BTS", _tagCompanyCustomTagModel.Bts.ToString(CultureInfo.InvariantCulture));
        UnitVitalityHeader = vitalityHeader;
        UnitVitality = UnitStatline.ReadValue(statline, vitalityHeader, _tagCompanyCustomTagModel.Vitality.ToString(CultureInfo.InvariantCulture));
        UnitS = UnitStatline.ReadValue(statline, "S", _tagCompanyCustomTagModel.Silhouette.ToString(CultureInfo.InvariantCulture));
        UnitAva = _tagCompanyCustomTagModel.Availability.ToString(CultureInfo.InvariantCulture);

        var equipmentText = TagCompanyCustomTagModel.NormalizeProfileText(entry.SavedEquipment);
        var skillsText = TagCompanyCustomTagModel.NormalizeProfileText(entry.SavedSkills);
        EquipmentSummary = $"Equipment: {equipmentText}";
        SpecialSkillsSummary = $"Special Skills: {skillsText}";
        EquipmentSummaryFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", equipmentText, Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Special Skills", skillsText, Color.FromArgb("#F59E0B"));
        Profiles.Clear();
        ProfilesStatus = $"Use 'Configure TAG (Spec Ops)' to spend up to {TagCompanyCustomTagModel.SpecOpsExperienceBudget} XP.";

        ShowRegularOrderIcon = true;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = true;

        ClearSelectedUnitLogoCore(UnitDisplayConfigurationsView);
        UnitDisplayConfigurationsView.SelectedUnitPicture = await CompanyUnitLogoWorkflowService.LoadSelectedUnitLogoAsync(
            entry.Name,
            entry.SourceUnitId,
            entry.SourceFactionId,
            async () => await FileSystem.Current.OpenAppPackageFileAsync(
                _tagCompanyCustomTagModel.ResolvePackagedLogoPath(entry.PackagedLogoPath)));
        UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
    }

    private async Task ShowInitialTagCompanyStateAsync(CancellationToken cancellationToken = default)
    {
        if (!ShowTagCompanyCustomTagControls || _selectedUnit is not null || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        EnsureTagCompanyCustomTagEntry();
        var entry = GetTagCompanyCustomTagEntry();
        if (entry is null)
        {
            return;
        }

        await ShowTagCompanyCustomTagDetailsAsync(entry);
    }

    private async void OnConfigureTagSpecOpsClicked(object? sender, EventArgs e)
    {
        await ConfigureTagCompanyCustomTagAsync();
    }
}
