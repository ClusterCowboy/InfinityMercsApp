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
    }
}

