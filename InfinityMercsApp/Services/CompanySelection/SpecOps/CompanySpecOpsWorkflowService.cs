using TemplateCaptain = InfinityMercsApp.Views.Common.Captain;
using TagGen = InfinityMercsApp.Infrastructure.Providers.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Views.Common.UICommon;

internal static class CompanySpecOpsWorkflowService
{
    private static readonly string[] DisallowedTagSpecOpsOptions =
    [
        "Forward Deployment",
        "Strategic Deployment",
        "Infiltration",
        "Impersonation",
        "Parachutist",
        "Combat Drop",
        "Engineer"
    ];

    public static async Task<TemplateCaptain.SavedImprovedCaptainStats?> ShowTagSpecOpsConfigurationAsync(
        CompanySpecOpsWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = ResolveSourceFactionIdForTagWorkflow(request);
        var candidateFactionIds = BuildFactionCandidateChain(sourceFactionId, request.TryGetParentFactionId);
        var mergedWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mergedSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mergedEquipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var optionFactionId = sourceFactionId;

        foreach (var candidateFactionId in candidateFactionIds)
        {
            var candidateOptions = await TemplateCaptain.CaptainPopupInputBuilder.LoadUpgradeOptionsAsync(
                request.ArmyDataService,
                request.SpecOpsProvider,
                candidateFactionId,
                request.ShowUnitsInInches,
                cancellationToken);

            if (!candidateOptions.IsEmpty &&
                mergedWeapons.Count == 0 &&
                mergedSkills.Count == 0 &&
                mergedEquipment.Count == 0)
            {
                optionFactionId = candidateFactionId;
            }

            foreach (var weapon in candidateOptions.Weapons)
            {
                mergedWeapons.Add(weapon);
            }

            foreach (var skill in candidateOptions.Skills)
            {
                mergedSkills.Add(skill);
            }

            foreach (var equipment in candidateOptions.Equipment)
            {
                mergedEquipment.Add(equipment);
            }
        }

        var options = FilterDisallowedTagSpecOpsOptions(new TemplateCaptain.CaptainUpgradeOptionSet
        {
            Weapons = mergedWeapons.ToList(),
            Skills = mergedSkills.ToList(),
            Equipment = mergedEquipment.ToList()
        });

        var context = new TemplateCaptain.CaptainUpgradePopupContext
        {
            Unit = new TemplateCaptain.CaptainUnitPopupInfo
            {
                Name = request.UnitName,
                Cost = request.UnitCost,
                Statline = request.UnitStatline,
                RangedWeapons = request.UnitRangedWeapons,
                CcWeapons = request.UnitCcWeapons,
                Skills = request.UnitSkills,
                Equipment = request.UnitEquipment,
                CachedLogoPath = request.UnitCachedLogoPath,
                PackagedLogoPath = request.UnitPackagedLogoPath
            },
            OptionFactionId = optionFactionId,
            OptionFactionName = ResolveOptionFactionName(request, sourceFactionId, optionFactionId),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment,
            PopupTitle = request.IsLieutenant ? "TAG Lieutenant Spec Ops" : "TAG Spec Ops",
            ConfirmButtonText = "Finalize Tag",
            DefaultUnitCustomName = request.IsLieutenant ? "Lieutenant" : "TAG",
            BaseExperienceOverride = Math.Max(0, request.BaseExperience)
        };

        return await TemplateCaptain.ConfigureCaptainPopupPage.ShowAsync(request.Navigation, context);
    }

    public static async Task<TStats?> ShowTagSpecOpsConfigurationAsync<TStats>(
        CompanySpecOpsWorkflowRequest request,
        CancellationToken cancellationToken = default)
        where TStats : class
    {
        var templateResult = await ShowTagSpecOpsConfigurationAsync(request, cancellationToken);
        if (templateResult is null)
        {
            return null;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(templateResult);
        return System.Text.Json.JsonSerializer.Deserialize<TStats>(json);
    }

    private static int ResolveNonTagSourceFactionId(int resolvedSourceFactionId, int? firstSourceFactionId)
    {
        if (resolvedSourceFactionId > 0 && resolvedSourceFactionId != TagGen.TagCompanyFactionId)
        {
            return resolvedSourceFactionId;
        }

        if (firstSourceFactionId.GetValueOrDefault() > 0 &&
            firstSourceFactionId.GetValueOrDefault() != TagGen.TagCompanyFactionId)
        {
            return firstSourceFactionId.GetValueOrDefault();
        }

        return resolvedSourceFactionId;
    }

    private static int ResolveSourceFactionIdForTagWorkflow(CompanySpecOpsWorkflowRequest request)
    {
        if (request.PreferredOtherFactionId.GetValueOrDefault() > 0 &&
            request.PreferredOtherFactionId.GetValueOrDefault() != TagGen.TagCompanyFactionId)
        {
            return request.PreferredOtherFactionId.GetValueOrDefault();
        }

        var rawSourceFactionId = TemplateCaptain.CaptainPopupInputBuilder.ResolveSourceFactionId(
            request.FallbackSourceFactionId,
            request.FirstSourceFactionId);
        return ResolveNonTagSourceFactionId(
            rawSourceFactionId,
            request.FirstSourceFactionId);
    }

    private static IReadOnlyList<int> BuildFactionCandidateChain(
        int sourceFactionId,
        Func<int, int?> tryGetParentFactionId)
    {
        var candidates = new List<int>();
        var seen = new HashSet<int>();
        var current = sourceFactionId;
        var guard = 0;

        while (current > 0 && seen.Add(current) && guard++ < 16)
        {
            candidates.Add(current);
            var parent = tryGetParentFactionId(current);
            if (!parent.HasValue || parent.Value <= 0 || parent.Value == current)
            {
                break;
            }

            current = parent.Value;
        }

        return candidates;
    }

    private static TemplateCaptain.CaptainUpgradeOptionSet FilterDisallowedTagSpecOpsOptions(
        TemplateCaptain.CaptainUpgradeOptionSet source)
    {
        static bool IsDisallowed(string optionLabel)
        {
            if (string.IsNullOrWhiteSpace(optionLabel))
            {
                return false;
            }

            foreach (var disallowed in DisallowedTagSpecOpsOptions)
            {
                if (optionLabel.Contains(disallowed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        return new TemplateCaptain.CaptainUpgradeOptionSet
        {
            Weapons = source.Weapons.Where(option => !IsDisallowed(option)).ToList(),
            Skills = source.Skills.Where(option => !IsDisallowed(option)).ToList(),
            Equipment = source.Equipment.Where(option => !IsDisallowed(option)).ToList()
        };
    }

    private static string ResolveOptionFactionName(
        CompanySpecOpsWorkflowRequest request,
        int sourceFactionId,
        int optionFactionId)
    {
        var sourceName = sourceFactionId > 0 ? request.TryGetFactionName(sourceFactionId) : null;
        var optionName = optionFactionId > 0 ? request.TryGetFactionName(optionFactionId) : null;
        var metadataSourceName = sourceFactionId > 0 ? request.TryGetMetadataFactionName(sourceFactionId) : null;
        var metadataOptionName = optionFactionId > 0 ? request.TryGetMetadataFactionName(optionFactionId) : null;
        return TemplateCaptain.CaptainPopupInputBuilder.ResolveOptionFactionName(
            sourceFactionId,
            optionFactionId,
            sourceName,
            optionName,
            metadataSourceName,
            metadataOptionName);
    }
}
