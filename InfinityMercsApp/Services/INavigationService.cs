namespace InfinityMercsApp.Services;

public interface INavigationService
{
    Task InitializeAsync();

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null);

    Task PopAsync();
}
