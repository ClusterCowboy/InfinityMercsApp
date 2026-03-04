namespace InfinityMercsApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.CreateNewArmyPage), typeof(Views.CreateNewArmyPage));
		Routing.RegisterRoute(nameof(Views.LoadCompanyPage), typeof(Views.LoadCompanyPage));
		Routing.RegisterRoute(nameof(Views.CompanyViewerPage), typeof(Views.CompanyViewerPage));
		Routing.RegisterRoute(nameof(Views.MercsGlossaryPage), typeof(Views.MercsGlossaryPage));
	}
}
