using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.ViewModels;

public partial class StandardCompanySourcePopupPageViewModel(INavigationService navigationService) : ViewModelBase(navigationService)
{
    [RelayCommand]
    private async Task ClosePopupAsync()
    {
        await NavigationService.PopModalAsync();
    }

    [RelayCommand]
    private async Task VanillaFactionSelectedAsync()
    {
        Console.WriteLine("[StandardCompanySourcePopupPage] Selected source: One Vanilla Faction");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.VanillaFactions);
    }

    [RelayCommand]
    private async Task TwoSectorialsSelectedAsync()
    {
        Console.WriteLine("[StandardCompanySourcePopupPage] Selected source: Two Sectorials");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.Sectorials);
    }

    private async Task NavigateToFactionSelectionAsync(ArmySourceSelectionMode mode)
    {
        try
        {
            await Shell.Current.Navigation.PushAsync(new StandardCompanySelectionPage(mode));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StandardCompanySourcePopupPage] Failed to navigate to faction selection page: {ex.Message}");
        }
    }
}
