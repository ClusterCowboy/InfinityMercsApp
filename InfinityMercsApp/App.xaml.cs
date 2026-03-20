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
		return new Window(_splashPage);
	}


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
