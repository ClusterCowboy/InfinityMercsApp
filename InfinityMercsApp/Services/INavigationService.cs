namespace InfinityMercsApp.Services;

public interface INavigationService
{
    Task InitializeAsync();

    Task PushAsync(Page page);

    Task PopAsync();
}
