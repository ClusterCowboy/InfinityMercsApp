namespace InfinityMercsApp.Services;

/// <inheritdoc/>
internal sealed class MauiNavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private Shell? GetActiveShell()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current;
        }

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return null;
        }

        if (window.Page is Shell existingShell)
        {
            return existingShell;
        }

        if (serviceProvider.GetService(typeof(AppShell)) is AppShell appShell)
        {
            window.Page = appShell;
            return appShell;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await NavigateToAsync("SplashPage");
    }

    /// <inheritdoc/>
    public Task PopAsync()
    {
        var shell = GetActiveShell()
            ?? throw new InvalidOperationException("No active Shell is available for navigation.");
        return shell.GoToAsync("..");
    }

    /// <inheritdoc/>
    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        var shell = GetActiveShell()
            ?? throw new InvalidOperationException("No active Shell is available for navigation.");
        var shellNavigation = new ShellNavigationState(route);

        return routeParameters != null
            ? shell.GoToAsync(shellNavigation, routeParameters)
            : shell.GoToAsync(shellNavigation);
    }

    /// <inheritdoc/>
    public async Task PushModalAsync(Page page)
    {
        var shell = GetActiveShell()
            ?? throw new InvalidOperationException("No active Shell is available for modal navigation.");
        var currentPage = shell.CurrentPage;
        await currentPage.Navigation.PushModalAsync(page);
    }

    /// <inheritdoc/>
    public async Task PopModalAsync()
    {
        var shell = GetActiveShell()
            ?? throw new InvalidOperationException("No active Shell is available for modal navigation.");
        var currentPage = shell.CurrentPage;
        await currentPage.Navigation.PopModalAsync();
    }
}
