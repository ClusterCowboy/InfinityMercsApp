namespace InfinityMercsApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.SplashPage), typeof(Views.SplashPage));
        Routing.RegisterRoute(nameof(Views.CreateNewCompanyPage), typeof(Views.CreateNewCompanyPage));
        Routing.RegisterRoute(nameof(Views.StandardCompanySourcePopupPage), typeof(Views.StandardCompanySourcePopupPage));
        Routing.RegisterRoute(nameof(Views.ModeSelectionPage), typeof(Views.ModeSelectionPage));
        Routing.RegisterRoute(nameof(Views.LoadCompanyPage), typeof(Views.LoadCompanyPage));
        Routing.RegisterRoute(nameof(Views.CompanyViewerPage), typeof(Views.CompanyViewerPage));
        Routing.RegisterRoute(nameof(Views.MercsGlossaryPage), typeof(Views.MercsGlossaryPage));
        Routing.RegisterRoute(nameof(Views.UnitEncyclopedia.UnitEncyclopediaPage), typeof(Views.UnitEncyclopedia.UnitEncyclopediaPage));
        Routing.RegisterRoute(nameof(Views.FeedbackBugsPage), typeof(Views.FeedbackBugsPage));
    }
}

