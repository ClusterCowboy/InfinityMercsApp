using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class SplashPageViewModel(
    IImportService importService,
    IFactionProvider factionProvider,
    INavigationService navigationService) : ViewModelBase(navigationService)
{
    private string _updateProgressMessage = string.Empty;

    public string UpdateProgressMessage
    {
        get => _updateProgressMessage;
        set => SetProperty(ref _updateProgressMessage, value);
    }

    public async override Task InitializeAsync()
    {
        // On the very first launch there is no cached army data, so we must finish the
        // import before showing the main page or it would be empty. On every later launch
        // the DB already holds data, so navigate immediately and let the daily sync (which
        // is network-bound and can be slow) run in the background.
        var hasCachedData = factionProvider.GetStoredFactionIds().Count > 0;

        if (hasCachedData)
        {
            WeakReferenceMessenger.Default.Send(new SplashCompletedMessage());
            await NavigationService.NavigateToAsync("//MercsSeasonPage");
            _ = UpdateAllDataAsync();
            return;
        }

        await UpdateAllDataAsync();
        WeakReferenceMessenger.Default.Send(new SplashCompletedMessage());
        await NavigationService.NavigateToAsync("//MercsSeasonPage");
    }

    private async Task<bool> UpdateAllDataAsync()
    {
        try
        {
            await foreach (var result in importService.ImportAllDataAsync())
            {
                UpdateProgressMessage = result.Message;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateAllDataAsync failed: {ex.Message}");
            UpdateProgressMessage = $"Update failed: {ex.Message}";
            return true;
        }
    }
}
