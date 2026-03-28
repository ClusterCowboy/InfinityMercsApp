using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class TagCompanySourcePopupPageViewModel(
    INavigationService navigationService,
    ICompanySelectionPageFactory companySelectionPageFactory) : ViewModelBase(navigationService)
{
    [RelayCommand]
    private async Task ClosePopupAsync()
    {
        await NavigationService.NavigateToAsync("//CreateNewCompanyPage");
    }

    [RelayCommand]
    private async Task VanillaFactionSelectedAsync()
    {
        Console.WriteLine("[TagCompanySourcePopupPage] Selected source: One Vanilla Faction");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.VanillaFactions);
    }

    [RelayCommand]
    private async Task TwoSectorialsSelectedAsync()
    {
        Console.WriteLine("[TagCompanySourcePopupPage] Selected source: Two Sectorials");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.Sectorials);
    }

    private async Task NavigateToFactionSelectionAsync(ArmySourceSelectionMode mode)
    {
        try
        {
            await Shell.Current.Navigation.PushAsync(companySelectionPageFactory.CreateTag(mode));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TagCompanySourcePopupPage] Failed to navigate to faction selection page: {ex.Message}");
        }
    }
}
