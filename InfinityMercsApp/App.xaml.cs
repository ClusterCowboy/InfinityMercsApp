using InfinityMercsApp.Views;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace InfinityMercsApp;

public partial class App : Application
{
	private readonly SplashPage _splashPage;

	public App(SplashPage splashPage)
	{
		_splashPage = splashPage;
		CrashLog.Initialize();

		// Datapad identity is dark-only; pin the theme so system-drawn
		// controls (pickers, dialogs) don't flip to a light variant.
		UserAppTheme = AppTheme.Dark;

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			CrashLog.Write("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject as Exception);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			CrashLog.Write("TaskScheduler.UnobservedTaskException", args.Exception);
		};

		// Intentionally NOT subscribing to FirstChanceException: it fires for every
		// caught exception anywhere in the framework (and MAUI/Android throw many during
		// startup). Logging each one synchronously to a file under a lock taxed cold start
		// and steady-state alike. UnhandledException/UnobservedTaskException cover real crashes.

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
				sb.AppendLine("================================================================");
				sb.AppendLine($"[{DateTimeOffset.Now:O}] {source}");

				// Only real crashes (those carrying an exception) get the full diagnostic
				// dump; informational writes like the init line stay terse.
				if (exception is not null)
				{
					AppendSystemReport(sb);
					sb.AppendLine();
					sb.AppendLine("----- Exception -----");
					AppendExceptionReport(sb, exception, depth: 0);
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

		// Snapshot of the app/device/runtime state at crash time. Every field is fetched
		// behind its own guard so that one unavailable value can't suppress the rest.
		private static void AppendSystemReport(StringBuilder sb)
		{
			sb.AppendLine("----- Environment -----");
			TryAppend(sb, "App", () =>
				$"{AppInfo.Current.Name} {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString}), package {AppInfo.Current.PackageName}");
			TryAppend(sb, "Device", () =>
				$"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} \"{DeviceInfo.Current.Name}\" ({DeviceInfo.Current.Idiom}, {DeviceInfo.Current.DeviceType})");
			TryAppend(sb, "Platform", () =>
				$"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}");
			TryAppend(sb, "OS", () =>
				$"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
			TryAppend(sb, "Runtime", () =>
				$"{RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
			TryAppend(sb, "Display", () =>
			{
				var d = DeviceDisplay.Current.MainDisplayInfo;
				return $"{d.Width}x{d.Height} @ {d.Density}x, {d.Orientation}";
			});
			TryAppend(sb, "Culture", () =>
				$"{CultureInfo.CurrentCulture.Name} / UI {CultureInfo.CurrentUICulture.Name}");
			TryAppend(sb, "Memory", () =>
				$"working set {Environment.WorkingSet / (1024 * 1024)} MB, GC heap {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
			TryAppend(sb, "CPU", () => $"{Environment.ProcessorCount} logical processors");
			TryAppend(sb, "Thread", () =>
				$"#{Environment.CurrentManagedThreadId} (pool: {Thread.CurrentThread.IsThreadPoolThread})");
		}

		private static void TryAppend(StringBuilder sb, string label, Func<string> value)
		{
			try
			{
				sb.AppendLine($"{label}: {value()}");
			}
			catch (Exception ex)
			{
				sb.AppendLine($"{label}: <unavailable: {ex.Message}>");
			}
		}

		// Walks the full inner-exception chain (and every branch of an AggregateException)
		// dumping type, message, HResult, Source, the Data bag, and stack trace at each level.
		private static void AppendExceptionReport(StringBuilder sb, Exception exception, int depth)
		{
			var indent = new string(' ', depth * 2);

			sb.AppendLine($"{indent}[{depth}] {exception.GetType().FullName}: {exception.Message}");
			sb.AppendLine($"{indent}    HResult: 0x{exception.HResult:X8}");
			if (!string.IsNullOrEmpty(exception.Source))
			{
				sb.AppendLine($"{indent}    Source: {exception.Source}");
			}

			if (exception.Data.Count > 0)
			{
				sb.AppendLine($"{indent}    Data:");
				foreach (DictionaryEntry entry in exception.Data)
				{
					sb.AppendLine($"{indent}      {entry.Key} = {entry.Value}");
				}
			}

			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				sb.AppendLine($"{indent}    StackTrace:");
				sb.AppendLine(exception.StackTrace);
			}

			if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
			{
				for (var i = 0; i < aggregate.InnerExceptions.Count; i++)
				{
					sb.AppendLine($"{indent}    --- AggregateException inner [{i}] ---");
					AppendExceptionReport(sb, aggregate.InnerExceptions[i], depth + 1);
				}
			}
			else if (exception.InnerException is not null)
			{
				AppendExceptionReport(sb, exception.InnerException, depth + 1);
			}
		}
	}
}
