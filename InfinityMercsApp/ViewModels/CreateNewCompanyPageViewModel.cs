using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

public partial class CreateNewCompanyPageViewModel(INavigationService navigationService, IServiceProvider provider) : ViewModelBase(navigationService)
{
    [RelayCommand]
    public async Task NavigateToModeSelectionPageAsync()
    {
        await NavigationService.NavigateToAsync("//ModeSelectionPage");
    }

    [RelayCommand]
    public async Task OpenStandardCompanyPopupAsync()
    {
        // TODO: Encapsulate popups in a service.
        // Probably best done using the community toolkit.
        // Modals can't navigate to non-modals and navigating twice from a page is inconsistent
        // So this is a regular page for now
        var page = provider.GetService(typeof(StandardCompanySourcePopupPage)) as Page;
        await NavigationService.NavigateToAsync("//StandardCompanySourcePopupPage");
    }

    [RelayCommand]
    private async Task OpenCohesiveCompanyPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Cohesive Company selected.");
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
        Console.WriteLine("[CreateNewCompanyPage] TAG Company selected.");
    }

    [RelayCommand]
    private async Task OpenProxyPackPageAsync()
    {
        Console.WriteLine("[CreateNewCompanyPage] Proxy Pack selected.");
    }
}
