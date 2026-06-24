using InfinityMercsApp.Diagnostics;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.Views;

public abstract class ContentPageBase : ContentPage
{
    public ContentPageBase()
    {
        NavigationPage.SetBackButtonTitle(this, string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not IViewModelBase ivmb)
        {
            return;
        }

        // Profiling: time the async data-load that runs on appear.
        var timer = PerfLog.StartTimer();
        await ivmb.InitializeAsyncCommand.ExecuteAsync(null);
        PerfLog.Mark($"init {GetType().Name}", timer.ElapsedMilliseconds);
    }
}
