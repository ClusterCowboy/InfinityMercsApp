using InfinityMercsApp.Views;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.InspiringCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public interface ICompanySelectionPageFactory
{
    StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode);
    CohesiveCompanySelectionPage CreateCohesive(ArmySourceSelectionMode mode);
    InspiringCompanySelectionPage CreateInspiring();
}

