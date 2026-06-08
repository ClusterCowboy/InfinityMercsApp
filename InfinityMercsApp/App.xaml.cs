using InfinityMercsApp.Views;
using System.Text;
using System.Runtime.ExceptionServices;

namespace InfinityMercsApp;

public partial class App : Application
{
	private readonly SplashPage _splashPage;

	public App(SplashPage splashPage)
	{
		_splashPage = splashPage;
		CrashLog.Initialize();

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			CrashLog.Write("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject as Exception);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			CrashLog.Write("TaskScheduler.UnobservedTaskException", args.Exception);
		};

		AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
		{
			CrashLog.Write("AppDomain.CurrentDomain.FirstChanceException", args.Exception);
		};

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(_splashPage);
#if WINDOWS
		window.HandlerChanged += OnWindowHandlerChanged;
#endif
		return window;
	}

#if WINDOWS
	private static bool _shellNavigatedSubscribed;

	private static void OnWindowHandlerChanged(object? sender, EventArgs e)
	{
		if (sender is not Window mauiWindow) return;
		if (mauiWindow.Handler?.PlatformView is not Microsoft.UI.Xaml.Window winuiWindow) return;

		void ScheduleHide()
		{
			// The CommandBar template is applied asynchronously.
			// Retry every 150 ms until we find and collapse the MoreButton (max 3 s).
			var attempts = 0;
			var timer = winuiWindow.DispatcherQueue.CreateTimer();
			timer.Interval = TimeSpan.FromMilliseconds(150);
			timer.IsRepeating = true;
			timer.Tick += (t, _) =>
			{
				if (HideMoreButtons(winuiWindow.Content) || ++attempts >= 20)
				{
					t.Stop();
					// Subscribe to Shell navigation once it's available so the button
					// is re-hidden whenever the CommandBar is updated after a navigation.
					if (!_shellNavigatedSubscribed && Shell.Current is { } shell)
					{
						_shellNavigatedSubscribed = true;
						shell.Navigated += (_, _) => winuiWindow.DispatcherQueue.TryEnqueue(
							Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
							ScheduleHide);
					}
				}
			};
			timer.Start();
		}

		winuiWindow.Activated += (_, _) => ScheduleHide();
	}

	// Returns true when at least one MoreButton/CommandBar was found and collapsed.
	private static bool HideMoreButtons(Microsoft.UI.Xaml.DependencyObject? node)
	{
		if (node is null) return false;
		var found = false;
		for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, i);
			if (child is Microsoft.UI.Xaml.Controls.CommandBar cb)
			{
				cb.OverflowButtonVisibility = Microsoft.UI.Xaml.Controls.CommandBarOverflowButtonVisibility.Collapsed;
				found = true;
			}
			if (child is Microsoft.UI.Xaml.FrameworkElement fe && fe.Name == "MoreButton")
			{
				fe.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
				found = true;
			}
			if (HideMoreButtons(child)) found = true;
		}
		return found;
	}
#endif


	internal static class CrashLog
	{
		private static readonly object Sync = new();
		private static string _logPath = "crash.log";

		public static string LogPath => _logPath;

		public static void Initialize()
		{
			try
			{
				_logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "runtime-crash.log");
				Write("Crash logger initialized.");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"CrashLog.Initialize failed: {ex.Message}");
			}
		}

		public static void Write(string source, Exception? exception = null)
		{
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine($"[{DateTimeOffset.UtcNow:O}] {source}");
				if (exception is not null)
				{
					sb.AppendLine(exception.ToString());
				}
				sb.AppendLine();

				lock (Sync)
				{
					File.AppendAllText(_logPath, sb.ToString());
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"CrashLog.Write failed: {ex.Message}");
			}
		}
	}
}
