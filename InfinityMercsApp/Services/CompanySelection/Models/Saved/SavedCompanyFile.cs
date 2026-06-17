namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Concrete saved-company DTO shared by every company type. All company pages persist the same
/// JSON shape, and all loaders (season, viewer, load, play/game mode) deserialize into this type,
/// so a single shared set replaces the previously duplicated per-page <c>Saved*</c> definitions.
/// </summary>
public sealed class SavedCompanyFile : CompanySavedCompanyFileBase<SavedImprovedCaptainStats, SavedCompanyFaction, SavedCompanyEntry>
{
}

public sealed class SavedImprovedCaptainStats : CompanySavedImprovedCaptainStatsBase
{
}

public sealed class SavedCompanyFaction : CompanySavedCompanyFactionBase
{
}

public sealed class SavedCompanyEntry : CompanySavedCompanyEntryBase
{
}
