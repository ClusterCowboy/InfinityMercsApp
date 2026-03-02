namespace InfinityMercsApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.CreateNewArmyPage), typeof(Views.CreateNewArmyPage));
		Routing.RegisterRoute(nameof(Views.LoadCompanyPage), typeof(Views.LoadCompanyPage));
	}
}
