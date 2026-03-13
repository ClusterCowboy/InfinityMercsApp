using InfinityMercsApp.Views;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public interface ICompanySelectionPageFactory
{
    StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode);
    CCArmyFactionSelectionPage CreateCohesive(ArmySourceSelectionMode mode);
}
