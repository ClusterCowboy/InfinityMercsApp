namespace InfinityMercsApp;

public partial class SplashPage : ContentPage
{
	private bool _navigated;

	public SplashPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_navigated)
		{
			return;
		}

		_navigated = true;

		await Task.Delay(2000);

		if (Application.Current?.Windows.Count > 0)
		{
			Application.Current.Windows[0].Page = new AppShell();
		}
	}
}
