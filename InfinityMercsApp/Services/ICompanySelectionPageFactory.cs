using InfinityMercsApp.Views;
using InfinityMercsApp.Views.AirborneCompany;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.InspiringCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public interface ICompanySelectionPageFactory
{
    StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode);
    StandardCompanySelectionPage CreateTag(ArmySourceSelectionMode mode);
    CohesiveCompanySelectionPage CreateCohesive(ArmySourceSelectionMode mode);
    InspiringCompanySelectionPage CreateInspiring();
    AirborneCompanySelectionPage CreateAirborne();
}

