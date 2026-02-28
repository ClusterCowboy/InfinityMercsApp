namespace InfinityMercsApp.Views;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnNewArmyClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(CreateNewArmyPage));
	}
}
