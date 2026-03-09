namespace InfinityMercsApp.Services;

internal class MauiNavigationService : INavigationService
{
    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public async Task PopAsync()
    {
        if (Application.Current?.Windows[0].Page is NavigationPage navigationPage)
        {
            await navigationPage.PopAsync();
        }
    }

    public async Task PushAsync(Page page)
    {
        if (Application.Current?.Windows[0].Page is NavigationPage navigationPage)
        {
            await navigationPage.PushAsync(page);
        }
    }
}
