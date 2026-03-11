using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		BindingContext = Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>()
			?? new MainViewModel();
	}
}
