using System.Text.Json;
using TemplateCaptain = InfinityMercsApp.Views.Common.Captain;

namespace InfinityMercsApp.Views.Common.UICommon;

internal static class CompanyCaptainWorkflowService
{
    public static async Task<TemplateCaptain.SavedImprovedCaptainStats?> ShowCaptainConfigurationAsync(
        CompanyCaptainWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = TemplateCaptain.CaptainPopupInputBuilder.ResolveSourceFactionId(
            request.FallbackSourceFactionId,
            request.FirstSourceFactionId);
        var optionFactionId = TemplateCaptain.CaptainPopupInputBuilder.ResolveOptionFactionId(
            sourceFactionId,
            request.TryGetParentFactionId(sourceFactionId));
        var options = await TemplateCaptain.CaptainPopupInputBuilder.LoadUpgradeOptionsAsync(
            request.ArmyDataService,
            request.SpecOpsProvider,
            optionFactionId,
            request.ShowUnitsInInches,
            cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = await TemplateCaptain.CaptainPopupInputBuilder.LoadUpgradeOptionsAsync(
                request.ArmyDataService,
                request.SpecOpsProvider,
                sourceFactionId,
                request.ShowUnitsInInches,
                cancellationToken);
            optionFactionId = sourceFactionId;
        }

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
            EquipmentOptions = options.Equipment
        };

        return await TemplateCaptain.ConfigureCaptainPopupPage.ShowAsync(request.Navigation, context);
    }

    public static async Task<TStats?> ShowCaptainConfigurationAsync<TStats>(
        CompanyCaptainWorkflowRequest request,
        CancellationToken cancellationToken = default)
        where TStats : class
    {
        var templateResult = await ShowCaptainConfigurationAsync(request, cancellationToken);
        if (templateResult is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(templateResult);
        return JsonSerializer.Deserialize<TStats>(json);
    }

    private static string ResolveOptionFactionName(
        CompanyCaptainWorkflowRequest request,
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
