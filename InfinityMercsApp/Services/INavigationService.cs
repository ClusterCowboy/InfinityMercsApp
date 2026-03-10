namespace InfinityMercsApp.Services;

/// <summary>
/// A service to handle navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();

    /// <summary>
    /// Navigates to a page.
    /// </summary>
    /// <param name="route"></param>
    /// <param name="routeParameters"></param>
    /// <returns></returns>
    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    /// <returns></returns>
    Task PopAsync();

    /// <summary>
    /// Pushes a modal.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    Task PushModalAsync(Page page);

    /// <summary>
    /// Pops a modal;
    /// </summary>
    /// <returns></returns>
    Task PopModalAsync();
}
