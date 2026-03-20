using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class CreateNewCompanyPageViewModel(
    INavigationService navigationService,
    ICompanySelectionPageFactory companySelectionPageFactory) : ViewModelBase(navigationService)
{
    [RelayCommand]
    public async Task NavigateToModeSelectionPageAsync()
    {
        await NavigationService.NavigateToAsync("//ModeSelectionPage");
    }

    [RelayCommand]
    public async Task OpenStandardCompanyPopupAsync()
    {
        await NavigationService.NavigateToAsync("//StandardCompanySourcePopupPage");
    }

    [RelayCommand]
    private async Task OpenCohesiveCompanyPageAsync()
    {
        await Shell.Current.Navigation.PushAsync(companySelectionPageFactory.CreateCohesive(ArmySourceSelectionMode.Sectorials));
    }

    [RelayCommand]
    private async Task OpenInspiringLeadershipPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Inspiring Leader selected.");
    }

    [RelayCommand]
    private async Task OpenAirborneCompanyPage()
    {
        Console.WriteLine("[CreateNewCompanyPage] Airborne Company selected.");
    }

    [RelayCommand]
    private async Task OpenTAGCompanyPageAsync()
    {
        await Shell.Current.Navigation.PushAsync(companySelectionPageFactory.CreateStandard(ArmySourceSelectionMode.TagSingleSource));
    }

    [RelayCommand]
    private async Task OpenProxyPackPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Proxy Pack selected.");
    }
}
