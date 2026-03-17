using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Templates.NewCompany;

internal readonly record struct CompanyHeaderColors(Color Primary, Color Secondary, Color PrimaryText, Color SecondaryText);

internal static class CompanySelectionVisualThemeWorkflow
{
    internal static async Task<string?> ResolveThemeFactionNameAsync(
        ArmySourceSelectionMode mode,
        IArmyDataService armyDataService,
        int sourceFactionId,
        string? unitFactionsJson,
        CancellationToken cancellationToken)
    {
        if (mode == ArmySourceSelectionMode.Sectorials)
        {
            return await ResolveVanillaFactionNameAsync(armyDataService, sourceFactionId, cancellationToken);
        }

        return await ResolveUnitVanillaFactionNameAsync(armyDataService, sourceFactionId, unitFactionsJson, cancellationToken);
    }

    internal static async Task<string?> ResolveUnitVanillaFactionNameAsync(
        IArmyDataService armyDataService,
        int sourceFactionId,
        string? unitFactionsJson,
        CancellationToken cancellationToken)
    {
        foreach (var factionId in CompanySelectionSharedUtilities.ParseFactionIds(unitFactionsJson))
        {
            var candidateName = await ResolveVanillaFactionNameAsync(armyDataService, factionId, cancellationToken);
            if (CompanySelectionSharedUtilities.IsThemeFactionName(candidateName))
            {
                return candidateName;
            }
        }

        return await ResolveVanillaFactionNameAsync(armyDataService, sourceFactionId, cancellationToken);
    }

    internal static Task<string?> ResolveVanillaFactionNameAsync(
        IArmyDataService armyDataService,
        int sourceFactionId,
        CancellationToken cancellationToken)
    {
        if (sourceFactionId <= 0)
        {
            return Task.FromResult<string?>(null);
        }

        var source = armyDataService.GetMetadataFactionById(sourceFactionId);
        FactionRecord? current = source is null
            ? null
            : new FactionRecord
            {
                Id = source.Id,
                ParentId = source.ParentId,
                Name = source.Name,
                Slug = source.Slug,
                Discontinued = source.Discontinued,
                Logo = source.Logo
            };

        var safety = 0;
        while (current is not null && safety < 8)
        {
            if (CompanySelectionSharedUtilities.IsThemeFactionName(current.Name))
            {
                return Task.FromResult<string?>(current.Name);
            }

            if (current.ParentId <= 0)
            {
                break;
            }

            var parentRecord = armyDataService.GetMetadataFactionById(current.ParentId);
            FactionRecord? parent = parentRecord is null
                ? null
                : new FactionRecord
                {
                    Id = parentRecord.Id,
                    ParentId = parentRecord.ParentId,
                    Name = parentRecord.Name,
                    Slug = parentRecord.Slug,
                    Discontinued = parentRecord.Discontinued,
                    Logo = parentRecord.Logo
                };

            if (parent is null || parent.Id == current.Id)
            {
                break;
            }

            current = parent;
            safety++;
        }

        var inferredThemeName = InferThemeFactionNameFromFactionId(sourceFactionId)
            ?? (current is not null ? InferThemeFactionNameFromFactionId(current.Id) : null);
        if (!string.IsNullOrWhiteSpace(inferredThemeName))
        {
            return Task.FromResult<string?>(inferredThemeName);
        }

        return Task.FromResult(current?.Name);
    }

    internal static CompanyHeaderColors GetHeaderColors(string? vanillaFactionName, Color defaultPrimary, Color defaultSecondary)
    {
        var (primary, secondary) = GetFactionTheme(vanillaFactionName, defaultPrimary, defaultSecondary);
        var primaryText = CompanySelectionSharedUtilities.IsLightColor(primary) ? Colors.Black : Colors.White;
        var secondaryText = CompanySelectionSharedUtilities.IsLightColor(secondary) ? Colors.Black : Colors.White;
        return new CompanyHeaderColors(primary, secondary, primaryText, secondaryText);
    }

    internal static (FormattedString EquipmentSummary, FormattedString SkillsSummary) BuildSummaryFormatted(
        UnitDisplayConfigurationsView unitDisplayConfigurationsView,
        Color secondaryBackgroundColor,
        bool highlightLieutenant)
    {
        var (equipmentAccent, skillsAccent) = GetSummaryAccentColorsForSecondaryBackground(secondaryBackgroundColor);
        var equipmentSummary = CompanyProfileTextService.BuildNamedSummaryFormatted(
            "Equipment",
            unitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            equipmentAccent);
        var skillsSummary = CompanyProfileTextService.BuildNamedSummaryFormatted(
            "Special Skills",
            unitDisplayConfigurationsView.SelectedUnitCommonSkills,
            skillsAccent,
            highlightLieutenantPurple: highlightLieutenant);
        return (equipmentSummary, skillsSummary);
    }

    internal static (Color EquipmentAccent, Color SkillsAccent) GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return CompanySelectionSharedUtilities.IsLightColor(secondaryBackground)
            ? (UnitDisplayConfigurationsView.EquipmentAccentOnLightSecondary, UnitDisplayConfigurationsView.SkillsAccentOnLightSecondary)
            : (UnitDisplayConfigurationsView.EquipmentAccentOnDarkSecondary, UnitDisplayConfigurationsView.SkillsAccentOnDarkSecondary);
    }

    internal static (Color Primary, Color Secondary) GetFactionTheme(string? factionName, Color defaultPrimary, Color defaultSecondary)
    {
        var key = CompanySelectionSharedUtilities.NormalizeFactionName(factionName);
        return key switch
        {
            "panoceania" => (Color.FromArgb("#239ac2"), Color.FromArgb("#006a91")),
            "yujing" => (Color.FromArgb("#ff9000"), Color.FromArgb("#995601")),
            "ariadna" => (Color.FromArgb("#007d27"), Color.FromArgb("#005825")),
            "haqqislam" => (Color.FromArgb("#e6da9b"), Color.FromArgb("#8a835d")),
            "nomads" => (Color.FromArgb("#ce181e"), Color.FromArgb("#7c0e13")),
            "combinedarmy" => (Color.FromArgb("#400b5f"), Color.FromArgb("#260739")),
            "aleph" => (Color.FromArgb("#aea6bb"), Color.FromArgb("#696471")),
            "tohaa" => (Color.FromArgb("#3b3b3b"), Color.FromArgb("#252525")),
            "nonalignedarmy" => (Color.FromArgb("#728868"), Color.FromArgb("#728868")),
            "o12" => (Color.FromArgb("#005470"), Color.FromArgb("#dead33")),
            "jsa" => (Color.FromArgb("#a6112b"), Color.FromArgb("#757575")),
            _ => (defaultPrimary, defaultSecondary)
        };
    }

    internal static string? InferThemeFactionNameFromFactionId(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        var family = factionId / 100;
        return family switch
        {
            1 => "PanOceania",
            2 => "Yu Jing",
            3 => "Ariadna",
            4 => "Haqqislam",
            5 => "Nomads",
            6 => "Combined Army",
            7 => "Aleph",
            8 => "Tohaa",
            9 => "Non-Aligned Armies",
            10 => "O-12",
            11 => "JSA",
            _ => null
        };
    }
}
