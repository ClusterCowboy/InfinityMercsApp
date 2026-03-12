namespace InfinityMercsApp.ViewModels.Base;

using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;

public interface IViewModelBase : IQueryAttributable
{
    public INavigationService NavigationService { get; }

    public IAsyncRelayCommand InitializeAsyncCommand { get; }

    public bool IsBusy { get; }

    public bool IsInitialized { get; }

    Task InitializeAsync();
}
