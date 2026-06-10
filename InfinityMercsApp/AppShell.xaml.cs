namespace InfinityMercsApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.SplashPage), typeof(Views.SplashPage));
        // Routes declared in AppShell.xaml ShellContent entries must not be registered here,
        // otherwise Shell can throw ambiguous-route exceptions.
        Routing.RegisterRoute(nameof(Views.FeedbackBugsPage), typeof(Views.FeedbackBugsPage));

        // Season flow — pushed as a stack on top of SeasonPage.
        Routing.RegisterRoute(nameof(Views.Season.PlayModePage), typeof(Views.Season.PlayModePage));
        Routing.RegisterRoute(nameof(Views.Season.GameModePage), typeof(Views.Season.GameModePage));
        Routing.RegisterRoute(nameof(Views.Season.InjuriesPage), typeof(Views.Season.InjuriesPage));
        Routing.RegisterRoute(nameof(Views.Season.ExperiencePage), typeof(Views.Season.ExperiencePage));
        Routing.RegisterRoute(nameof(Views.Season.DowntimePage), typeof(Views.Season.DowntimePage));

        // MERCS Season home sub-pages.
        Routing.RegisterRoute(nameof(Views.Season.LoadSeasonPage), typeof(Views.Season.LoadSeasonPage));
    }
}
