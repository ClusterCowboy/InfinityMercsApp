using TemplateCaptain = InfinityMercsApp.Views.Templates.Captain;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    private async Task<SavedImprovedCaptainStats?> ShowCaptainConfigurationAsync(MercsCompanyEntry captainEntry, CancellationToken cancellationToken = default)
    {
        var sourceFactionId = ResolveCaptainSourceFactionId(captainEntry.SourceFactionId);
        var optionFactionId = ResolveCaptainOptionFactionId(sourceFactionId);
        var options = await LoadCaptainUpgradeOptionsAsync(optionFactionId, cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = await LoadCaptainUpgradeOptionsAsync(sourceFactionId, cancellationToken);
            optionFactionId = sourceFactionId;
        }

        var unitInfo = new TemplateCaptain.CaptainUnitPopupInfo
        {
            Name = captainEntry.Name,
            Cost = captainEntry.CostValue,
            Statline = captainEntry.Subtitle ?? "-",
            RangedWeapons = captainEntry.SavedRangedWeapons,
            CcWeapons = captainEntry.SavedCcWeapons,
            Skills = captainEntry.SavedSkills,
            Equipment = captainEntry.SavedEquipment,
            CachedLogoPath = captainEntry.CachedLogoPath,
            PackagedLogoPath = captainEntry.PackagedLogoPath
        };

        var context = new TemplateCaptain.CaptainUpgradePopupContext
        {
            Unit = unitInfo,
            OptionFactionId = optionFactionId,
            OptionFactionName = await ResolveCaptainOptionFactionNameAsync(sourceFactionId, optionFactionId, cancellationToken),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment
        };

        var popupResult = await TemplateCaptain.ConfigureCaptainPopupPage.ShowAsync(Navigation, context);
        return popupResult is null ? null : MapTemplateCaptainResult(popupResult);
    }

    private static SavedImprovedCaptainStats MapTemplateCaptainResult(TemplateCaptain.SavedImprovedCaptainStats result)
    {
        return new SavedImprovedCaptainStats
        {
            IsEnabled = result.IsEnabled,
            CaptainName = result.CaptainName,
            CcTier = result.CcTier,
            BsTier = result.BsTier,
            PhTier = result.PhTier,
            WipTier = result.WipTier,
            ArmTier = result.ArmTier,
            BtsTier = result.BtsTier,
            VitalityTier = result.VitalityTier,
            CcBonus = result.CcBonus,
            BsBonus = result.BsBonus,
            PhBonus = result.PhBonus,
            WipBonus = result.WipBonus,
            ArmBonus = result.ArmBonus,
            BtsBonus = result.BtsBonus,
            VitalityBonus = result.VitalityBonus,
            WeaponChoice1 = result.WeaponChoice1,
            WeaponChoice2 = result.WeaponChoice2,
            WeaponChoice3 = result.WeaponChoice3,
            SkillChoice1 = result.SkillChoice1,
            SkillChoice2 = result.SkillChoice2,
            SkillChoice3 = result.SkillChoice3,
            EquipmentChoice1 = result.EquipmentChoice1,
            EquipmentChoice2 = result.EquipmentChoice2,
            EquipmentChoice3 = result.EquipmentChoice3,
            OptionFactionId = result.OptionFactionId,
            OptionFactionName = result.OptionFactionName
        };
    }

    private int ResolveCaptainSourceFactionId(int fallbackSourceFactionId)
    {
        var firstSource = GetUnitSourceFactions().FirstOrDefault();
        return TemplateCaptain.CaptainPopupInputBuilder.ResolveSourceFactionId(fallbackSourceFactionId, firstSource?.Id);
    }

    private int ResolveCaptainOptionFactionId(int sourceFactionId)
    {
        var sourceFaction = Factions.FirstOrDefault(x => x.Id == sourceFactionId);
        return TemplateCaptain.CaptainPopupInputBuilder.ResolveOptionFactionId(sourceFactionId, sourceFaction?.ParentId);
    }

    private Task<string> ResolveCaptainOptionFactionNameAsync(
        int sourceFactionId,
        int optionFactionId,
        CancellationToken cancellationToken)
    {
        var sourceName = sourceFactionId > 0 ? Factions.FirstOrDefault(x => x.Id == sourceFactionId)?.Name : null;
        var optionName = optionFactionId > 0 ? Factions.FirstOrDefault(x => x.Id == optionFactionId)?.Name : null;
        var metadataSourceName = sourceFactionId > 0 ? _armyDataService.GetMetadataFactionById(sourceFactionId)?.Name : null;
        var metadataOptionName = optionFactionId > 0 ? _armyDataService.GetMetadataFactionById(optionFactionId)?.Name : null;
        var resolved = TemplateCaptain.CaptainPopupInputBuilder.ResolveOptionFactionName(
            sourceFactionId,
            optionFactionId,
            sourceName,
            optionName,
            metadataSourceName,
            metadataOptionName);
        return Task.FromResult(resolved);
    }

    private async Task<CaptainUpgradeOptionSet> LoadCaptainUpgradeOptionsAsync(int factionId, CancellationToken cancellationToken)
    {
        var options = await TemplateCaptain.CaptainPopupInputBuilder.LoadUpgradeOptionsAsync(
            _armyDataService,
            _specOpsProvider,
            factionId,
            ShowUnitsInInches,
            cancellationToken);
        return new CaptainUpgradeOptionSet
        {
            Weapons = options.Weapons,
            Skills = options.Skills,
            Equipment = options.Equipment
        };
    }
}
