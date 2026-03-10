namespace InfinityMercsApp.Services;

internal class MauiNavigationService : INavigationService
{
    public async Task InitializeAsync()
    {
        await NavigateToAsync("SplashPage");
    }

    public Task PopAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        var shellNavigation = new ShellNavigationState(route);

        return routeParameters != null
            ? Shell.Current.GoToAsync(shellNavigation, routeParameters)
            : Shell.Current.GoToAsync(shellNavigation);
    }
}
