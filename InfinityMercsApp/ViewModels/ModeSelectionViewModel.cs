using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class ModeSelectionViewModel(INavigationService navigationService) : ViewModelBase(navigationService)
{
    [RelayCommand]
    public async Task NavigateToCreateNewArmyAsync()
    {
        await NavigationService.NavigateToAsync(nameof(CreateNewArmyPage));
    }

    [RelayCommand]
    public async Task NavigateToLoadArmyAsync()
    {
        await Shell.Current.GoToAsync(nameof(LoadCompanyPage));
    }
}