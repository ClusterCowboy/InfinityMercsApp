using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class CreateNewArmyPageViewModel(INavigationService navigationService) : ViewModelBase(navigationService)
{
    [RelayCommand]
    public async Task NavigateToModeSelectionPageAsync()
    {
        await NavigationService.NavigateToAsync("//ModeSelectionPage");
    }

    [RelayCommand]
    public async Task OpenStandardCompanyPopupAsync()
    {
        await NavigationService.PushModalAsync(new StandardCompanySourcePopupPage());
    }

    [RelayCommand]
    private async Task OpenCohesiveCompanyPageAsync()
    {
        Console.WriteLine("[CreateNewArmyPage] Cohesive Company selected.");
    }

    [RelayCommand]
    private async Task OpenInspiringLeadershipPageAsync()
    {
        Console.WriteLine("[CreateNewArmyPage] Inspiring Leader selected.");
    }

    [RelayCommand]
    private async Task OpenAirborneCompanyPage()
    {
        Console.WriteLine("[CreateNewArmyPage] Airborne Company selected.");
    }

    [RelayCommand]
    private async Task OpenTAGCompanyPageAsync()
    {
        Console.WriteLine("[CreateNewArmyPage] TAG Company selected.");
    }

    [RelayCommand]
    private async Task OpenProxyPackPageAsync()
    {
        Console.WriteLine("[CreateNewArmyPage] Proxy Pack selected.");
    }
}
