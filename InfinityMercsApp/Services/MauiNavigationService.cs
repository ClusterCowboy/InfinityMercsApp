namespace InfinityMercsApp.Services;

/// <inheritdoc/>
internal class MauiNavigationService : INavigationService
{
    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await NavigateToAsync("SplashPage");
    }

    /// <inheritdoc/>
    public Task PopAsync()
    {
        return Shell.Current.GoToAsync("..");
    }

    /// <inheritdoc/>
    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        var shellNavigation = new ShellNavigationState(route);

        return routeParameters != null
            ? Shell.Current.GoToAsync(shellNavigation, routeParameters)
            : Shell.Current.GoToAsync(shellNavigation);
    }

    /// <inheritdoc/>
    public async Task PushModalAsync(Page page)
    {
        var currentPage = Shell.Current.CurrentPage;
        await currentPage.Navigation.PushModalAsync(page);
    }

    /// <inheritdoc/>
    public async Task PopModalAsync()
    {
        var currentPage = Shell.Current.CurrentPage;
        await currentPage.Navigation.PopModalAsync();
    }
}
