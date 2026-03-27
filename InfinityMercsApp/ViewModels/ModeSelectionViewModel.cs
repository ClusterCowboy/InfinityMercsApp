using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class ModeSelectionViewModel(INavigationService navigationService) : ViewModelBase(navigationService)
{
    [RelayCommand]
    public async Task NavigateToCreateNewCompanyAsync()
    {
        await NavigationService.NavigateToAsync("//CreateNewCompanyPage");
    }

    [RelayCommand]
    public async Task NavigateToLoadArmyAsync()
    {
        await NavigationService.NavigateToAsync(nameof(LoadCompanyPage));
    }
}
