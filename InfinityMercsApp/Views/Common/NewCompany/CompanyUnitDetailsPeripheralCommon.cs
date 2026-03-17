using System.Text.Json;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views.Common.NewCompany;

internal static class CompanyUnitDetailsPeripheralCommon
{
    internal static CompanyPeripheralStatBlockResult? BuildPeripheralCore(
        string peripheralName,
        JsonElement peripheralProfile,
        string? filtersJson,
        bool showUnitsInInches,
        int? moveFirstCm,
        int? moveSecondCm)
    {
        return CompanyPeripheralStatBlockService.Build(new CompanyPeripheralStatBlockRequest
        {
            PeripheralName = peripheralName,
            PeripheralProfile = peripheralProfile,
            FiltersJson = filtersJson,
            ShowUnitsInInches = showUnitsInInches,
            MoveFirstCm = moveFirstCm,
            MoveSecondCm = moveSecondCm,
            TryParseId = CompanySelectionSharedUtilities.TryParseId,
            ReadVitality = CompanyUnitDetailsShared.ReadVitality,
            ReadMoveFromProfile = CompanyUnitDetailsShared.ReadMoveFromProfile,
            ReadIntAsString = CompanyUnitDetailsShared.ReadIntAsString,
            ReadAvaAsString = CompanyUnitDetailsShared.ReadAvaAsString
        });
    }
}

