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
		});
	}
#endif

	private static bool _resizeShortcutsRegistered;

	// Called once the home page (MercsSeasonPage) has appeared — by then the WinUI window,
	// its content, and the Shell are all live, which the splash-completed message timing did
	// not guarantee. Idempotent: only the first call wires up the shortcuts.
	internal static void EnsureWindowResizeShortcuts()
	{
		if (_resizeShortcutsRegistered) return;

		var winuiWindow = Microsoft.Maui.Controls.Application.Current?.Windows
			.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (winuiWindow is null) return;

		_resizeShortcutsRegistered = true;
		RegisterWindowResizeShortcuts(winuiWindow);
	}

	// The element the resize accelerators are attached to. MAUI swaps the WinUI root
	// content (Window.Content) when the Shell replaces the splash page, so accelerators
	// added to the old content are lost — we re-attach to whatever content is current.
	private static Microsoft.UI.Xaml.UIElement? _resizeShortcutHost;

	// Q/W/E/R target the adaptive width breakpoints (page width <600 / 600-899 / 900-1199 / >=1200),
	// laid out small→large across the QWERTY row:
	//   Ctrl+Shift+Q → 500×950 (Compact)   Ctrl+Shift+W → 750×950 (Medium)
	//   Ctrl+Shift+E → 1080×850 (Expanded) Ctrl+Shift+R → 1400×900 (Wide)
	// A/S/D/F/G mirror real devices (logical points/dp, portrait):
	//   Ctrl+Shift+A → 412×915 (Pixel 6)   Ctrl+Shift+S → 448×998 (Pixel 10 Pro XL)
	//   Ctrl+Shift+D → 375×812 (iPhone X)  Ctrl+Shift+F → 440×956 (iPhone 17 Pro Max)
	//   Ctrl+Shift+G → 800×1280 (Samsung Tab A9+)
	private static void RegisterWindowResizeShortcuts(Microsoft.UI.Xaml.Window winuiWindow)
	{
		// KeyboardAccelerators fire window-wide regardless of which element has focus,
		// unlike a routed KeyDown handler which only sees keys that bubble up from the
		// focused element — so they survive navigations landing focus outside our tree.
		void Attach()
		{
			var content = winuiWindow.Content;
			if (content is null || ReferenceEquals(content, _resizeShortcutHost)) return;
			_resizeShortcutHost = content;

			foreach (var key in new[]
			{
				Windows.System.VirtualKey.Q, Windows.System.VirtualKey.W,
				Windows.System.VirtualKey.E, Windows.System.VirtualKey.R,
				Windows.System.VirtualKey.A, Windows.System.VirtualKey.S,
				Windows.System.VirtualKey.D, Windows.System.VirtualKey.F,
				Windows.System.VirtualKey.G,
			})
			{
				var accelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
				{
					Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift,
					Key = key,
				};
				accelerator.Invoked += (_, args) =>
				{
					args.Handled = true;
					ResizeWindowTo(winuiWindow, args.KeyboardAccelerator.Key);
				};
				content.KeyboardAccelerators.Add(accelerator);
			}
		}

		// The Shell doesn't exist yet at splash-complete, so poll briefly until it does:
		// bind to the current content each tick, then subscribe to Navigated (which fires
		// after every content swap) and stop. Re-attach on each navigation thereafter.
		var attempts = 0;
		var timer = winuiWindow.DispatcherQueue.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(150);
		timer.IsRepeating = true;
		timer.Tick += (t, _) =>
		{
			Attach();
			if (Shell.Current is { } shell)
			{
				shell.Navigated += (_, _) => winuiWindow.DispatcherQueue.TryEnqueue(Attach);
				t.Stop();
			}
			else if (++attempts >= 40)
			{
				t.Stop();
			}
		};
		timer.Start();
	}

	private static void ResizeWindowTo(Microsoft.UI.Xaml.Window winuiWindow, Windows.System.VirtualKey key)
	{
		(int w, int h)? target = key switch
		{
			Windows.System.VirtualKey.Q => (500, 950),    // Compact  (<600)
			Windows.System.VirtualKey.W => (750, 950),    // Medium   (600-899)
			Windows.System.VirtualKey.E => (1080, 850),   // Expanded (900-1199)
			Windows.System.VirtualKey.R => (1400, 900),   // Wide     (>=1200)
			Windows.System.VirtualKey.A => (412, 915),    // Pixel 6
			Windows.System.VirtualKey.S => (448, 998),    // Pixel 10 Pro XL
			Windows.System.VirtualKey.D => (375, 812),    // iPhone X
			Windows.System.VirtualKey.F => (440, 956),    // iPhone 17 Pro Max
			Windows.System.VirtualKey.G => (800, 1280),   // Samsung Tab A9+
			_ => null
		};

		if (target is null) return;

		// Resize is a no-op while the window is maximized; restore first.
		if (winuiWindow.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter { State: Microsoft.UI.Windowing.OverlappedPresenterState.Maximized } presenter)
		{
			presenter.Restore();
		}

		var dpiScale = winuiWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
		var physW = (int)(target.Value.w * dpiScale);
		var physH = (int)(target.Value.h * dpiScale);
		winuiWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(physW, physH));
	}
}

