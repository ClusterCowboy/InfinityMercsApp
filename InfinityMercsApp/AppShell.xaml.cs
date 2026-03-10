using InfinityMercsApp.Services;

namespace InfinityMercsApp;

public partial class AppShell : Shell
{
	private readonly INavigationService _navigationService;

	public AppShell(INavigationService navigationService)
	{
        _navigationService = navigationService;

        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.SplashPage), typeof(Views.SplashPage));
		Routing.RegisterRoute(nameof(Views.CreateNewCompanyPage), typeof(Views.CreateNewCompanyPage));
        Routing.RegisterRoute(nameof(Views.StandardCompanySourcePopupPage), typeof(Views.StandardCompanySourcePopupPage));
		Routing.RegisterRoute(nameof(Views.ModeSelectionPage), typeof(Views.ModeSelectionPage));
		Routing.RegisterRoute(nameof(Views.LoadCompanyPage), typeof(Views.LoadCompanyPage));
		Routing.RegisterRoute(nameof(Views.CompanyViewerPage), typeof(Views.CompanyViewerPage));
		Routing.RegisterRoute(nameof(Views.MercsGlossaryPage), typeof(Views.MercsGlossaryPage));
		Routing.RegisterRoute(nameof(Views.FeedbackBugsPage), typeof(Views.FeedbackBugsPage));
	}

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is not null)
        {
            await _navigationService.InitializeAsync();
        }
    }
}
