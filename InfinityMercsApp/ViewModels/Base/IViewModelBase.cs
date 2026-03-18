using CommunityToolkit.Mvvm.Input;

namespace InfinityMercsApp.ViewModels.Base;

public interface IViewModelBase
{
    public bool IsInitialized { get; set; }

    public bool IsBusy { get; }

    public IAsyncRelayCommand InitializeAsyncCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query);
}
