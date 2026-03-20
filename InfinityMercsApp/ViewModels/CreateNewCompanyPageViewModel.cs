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
    private Task OpenInspiringLeadershipPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Inspiring Leader selected.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenAirborneCompanyPage()
    {
        Console.WriteLine("[CreateNewCompanyPage] Airborne Company selected.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenTAGCompanyPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] TAG Company selected.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenProxyPackPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Proxy Pack selected.");
        return Task.CompletedTask;
    }
}
