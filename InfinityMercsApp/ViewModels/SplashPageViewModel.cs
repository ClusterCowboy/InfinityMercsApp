using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class SplashPageViewModel(
    IImportService importService,
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
        var updated = await UpdateAllDataAsync();

        // Wait so the splash screen doesn't instantly disappear
        await Task.Delay(1000);

        if (updated)
        {
            WeakReferenceMessenger.Default.Send(new SplashCompletedMessage());
            await NavigationService.NavigateToAsync("//ModeSelectionPage");
        }
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
