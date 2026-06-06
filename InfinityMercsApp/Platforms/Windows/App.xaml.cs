using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace InfinityMercsApp.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
		this.UnhandledException += (_, args) =>
		{
			InfinityMercsApp.App.CrashLog.Write("WinUI.UnhandledException", args.Exception);
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

#if SIMULATE_TABLET
	protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{
		base.OnLaunched(args);

		WeakReferenceMessenger.Default.Register<SplashCompletedMessage>(this, (_, _) =>
		{
			WeakReferenceMessenger.Default.Unregister<SplashCompletedMessage>(this);

			var winuiWindow = Microsoft.Maui.Controls.Application.Current?.Windows
				.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

			if (winuiWindow is null) return;

			winuiWindow.DispatcherQueue.TryEnqueue(() =>
			{
				const int targetLogicalWidth = 800;
				const int targetLogicalHeight = 1280;

				// RasterizationScale converts logical pixels → physical pixels (e.g. 1.25 at 125% DPI)
				var dpiScale = winuiWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;

				var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
					winuiWindow.AppWindow.Id,
					Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

				var available = displayArea.WorkArea;
				var physicalTargetWidth  = (int)(targetLogicalWidth  * dpiScale);
				var physicalTargetHeight = (int)(targetLogicalHeight * dpiScale);

				var fitScale = Math.Min(
					(double)available.Width  / physicalTargetWidth,
					(double)available.Height / physicalTargetHeight);
				fitScale = Math.Min(fitScale, 1.0);

				var width  = (int)(physicalTargetWidth  * fitScale);
				var height = (int)(physicalTargetHeight * fitScale);

				winuiWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

				if (winuiWindow.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
				{
					presenter.IsResizable = false;
					presenter.IsMaximizable = false;
				}
			});

			RegisterWindowResizeShortcuts(winuiWindow);
		});
	}
#else
	protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
	{
		base.OnLaunched(args);

		WeakReferenceMessenger.Default.Register<SplashCompletedMessage>(this, (_, _) =>
		{
			WeakReferenceMessenger.Default.Unregister<SplashCompletedMessage>(this);

			var winuiWindow = Microsoft.Maui.Controls.Application.Current?.Windows
				.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

			if (winuiWindow is null) return;

			RegisterWindowResizeShortcuts(winuiWindow);
		});
	}
#endif

	// Ctrl+Shift+Q → 440×956   Ctrl+Shift+W → 414×921   Ctrl+Shift+E → 675×1080
	private static void RegisterWindowResizeShortcuts(Microsoft.UI.Xaml.Window winuiWindow)
	{
		if (winuiWindow.Content is null) return;

		winuiWindow.Content.AddHandler(
			Microsoft.UI.Xaml.UIElement.KeyDownEvent,
			new Microsoft.UI.Xaml.Input.KeyEventHandler((_, e) =>
			{
				var ctrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
					Windows.System.VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
				var shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
					Windows.System.VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

				if (!ctrl || !shift) return;

				(int w, int h)? target = e.Key switch
				{
					Windows.System.VirtualKey.Q => (440, 956),
					Windows.System.VirtualKey.W => (414, 921),
					Windows.System.VirtualKey.E => (675, 1080),
					_ => null
				};

				if (target is null) return;
				e.Handled = true;

				var dpiScale = winuiWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
				var physW = (int)(target.Value.w * dpiScale);
				var physH = (int)(target.Value.h * dpiScale);
				winuiWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(physW, physH));
			}),
			handledEventsToo: true);
	}
}

