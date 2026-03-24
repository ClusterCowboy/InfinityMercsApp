using System.Text.Json;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Services;

internal sealed class CompanyPeripheralStatBlockRequest
{
    public required string PeripheralName { get; init; }
    public required JsonElement PeripheralProfile { get; init; }
    public required string? FiltersJson { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required int? MoveFirstCm { get; init; }
    public required int? MoveSecondCm { get; init; }
    public required CompanyUnitDetailDisplayNameContext.TryParseIdDelegate TryParseId { get; init; }
    public required Func<JsonElement, (string VitalityHeader, string VitalityValue)> ReadVitality { get; init; }
    public required Func<JsonElement, string> ReadMoveFromProfile { get; init; }
    public required Func<JsonElement, string, string> ReadIntAsString { get; init; }
    public required Func<JsonElement, string> ReadAvaAsString { get; init; }
}

internal sealed class CompanyPeripheralStatBlockResult
{
    public string NameHeading { get; init; } = string.Empty;
    public int? MoveFirstCm { get; init; }
    public int? MoveSecondCm { get; init; }
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";
    public string VitalityHeader { get; init; } = "VITA";
    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Characteristics { get; init; } = "-";
}

internal static class CompanyPeripheralStatBlockService
{
    /// <summary>
    /// Builds a peripheral stat block from profile JSON and shared display-name rules.
    /// </summary>
    public static CompanyPeripheralStatBlockResult? Build(CompanyPeripheralStatBlockRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PeripheralName))
        {
            return null;
        }

        var equipLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(request.FiltersJson, "equip");
        var skillsLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(request.FiltersJson, "skills");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(request.FiltersJson, request.ShowUnitsInInches, request.TryParseId);
        var charsLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(request.FiltersJson, "chars");

        var equipmentNames = displayNameContext.GetOrderedIdDisplayNamesFromEntries(
            CompanySelectionSharedUtilities.GetContainerEntries(request.PeripheralProfile, "equip"),
            equipLookup);
        var skillNames = CompanyProfileTextService.BuildConfigurationSkillNames(
            displayNameContext.GetOrderedIdDisplayNamesFromEntries(
                CompanySelectionSharedUtilities.GetContainerEntries(request.PeripheralProfile, "skills"),
                skillsLookup));
        var characteristicNames = displayNameContext.GetOrderedIdDisplayNamesFromEntries(
            CompanySelectionSharedUtilities.GetContainerEntries(request.PeripheralProfile, "chars"),
            charsLookup);
        var (vitalityHeader, vitalityValue) = request.ReadVitality(request.PeripheralProfile);

        return new CompanyPeripheralStatBlockResult
        {
            NameHeading = $"Peripheral: {request.PeripheralName}",
            MoveFirstCm = request.MoveFirstCm,
            MoveSecondCm = request.MoveSecondCm,
            Mov = request.ReadMoveFromProfile(request.PeripheralProfile),
            Cc = request.ReadIntAsString(request.PeripheralProfile, "cc"),
            Bs = request.ReadIntAsString(request.PeripheralProfile, "bs"),
            Ph = request.ReadIntAsString(request.PeripheralProfile, "ph"),
            Wip = request.ReadIntAsString(request.PeripheralProfile, "wip"),
            Arm = request.ReadIntAsString(request.PeripheralProfile, "arm"),
            Bts = request.ReadIntAsString(request.PeripheralProfile, "bts"),
            VitalityHeader = vitalityHeader,
            Vitality = vitalityValue,
            S = request.ReadIntAsString(request.PeripheralProfile, "s"),
            Ava = request.ReadAvaAsString(request.PeripheralProfile),
            Equipment = CompanyProfileTextService.JoinOrDash(equipmentNames),
            Skills = CompanyProfileTextService.JoinOrDash(skillNames),
            Characteristics = CompanyProfileTextService.JoinOrDash(characteristicNames)
        };
    }
}



