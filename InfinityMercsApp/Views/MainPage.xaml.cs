using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = Application.Current?.Handler?.MauiContext?.Services.GetService<MainViewModel>()
			?? new MainViewModel();
	}
}
