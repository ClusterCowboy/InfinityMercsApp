using System.Diagnostics;

namespace InfinityMercsApp.Diagnostics;

/// <summary>
/// Lightweight, opt-in page navigation profiler. Emits one greppable line per
/// signal to the platform console (logcat tag <c>DOTNET</c> on Android), so slow
/// pages can be identified without a full profiler attach.
/// </summary>
/// <remarks>
/// Wire-up lives in <see cref="AppShell"/> (Shell navigation events) and
/// <c>ContentPageBase</c> (data-load timing). All output is prefixed with
/// <see cref="Tag"/> for easy filtering: <c>adb logcat | grep PERF</c>.
/// Remove the call sites to strip profiling entirely.
/// </remarks>
internal static class PerfLog
{
    /// <summary>Prefix on every emitted line. Filter with <c>grep PERF</c>.</summary>
    public const string Tag = "[PERF]";

    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    // Navigations are awaited sequentially, so a single in-flight nav is enough.
    private static long _navStartMs;
    private static string _navTarget = "?";

    /// <summary>Marks the start of a Shell navigation (from <c>Shell.Navigating</c>).</summary>
    public static void NavigationStarting(string target)
    {
        _navStartMs = Clock.ElapsedMilliseconds;
        _navTarget = string.IsNullOrWhiteSpace(target) ? "?" : target;
    }

    /// <summary>
    /// Marks navigation completion (from <c>Shell.Navigated</c>) and schedules a
    /// one-shot render measurement off the landed page's <see cref="VisualElement.Loaded"/>.
    /// </summary>
    public static void NavigationCompleted(Page? page)
    {
        var startMs = _navStartMs;
        var target = _navTarget;
        var pageName = page?.GetType().Name ?? target;

        var transitionMs = Clock.ElapsedMilliseconds - startMs;
        Write($"nav -> {target} ({pageName}) | transition={transitionMs}ms");

        if (page is null)
        {
            return;
        }

        // Loaded fires when the platform view enters the visual tree (first render).
        // It only fires on a fresh load; revisiting a cached page won't re-fire,
        // which is fine — first load is the expensive one we care about.
        void OnLoaded(object? sender, EventArgs e)
        {
            page.Loaded -= OnLoaded;
            var renderMs = Clock.ElapsedMilliseconds - startMs;
            Write($"page {pageName} | rendered={renderMs}ms");
        }

        page.Loaded += OnLoaded;
    }

    /// <summary>Times an arbitrary labelled async/sync block (e.g. a page's data load).</summary>
    public static void Mark(string label, long elapsedMs) =>
        Write($"{label} | elapsed={elapsedMs}ms");

    /// <summary>Returns a running stopwatch for ad-hoc timing; pair with <see cref="Mark"/>.</summary>
    public static Stopwatch StartTimer() => Stopwatch.StartNew();

    private static void Write(string message)
    {
        // Console.WriteLine surfaces under logcat tag DOTNET on Android;
        // Debug.WriteLine covers the IDE/Windows output window.
        Console.WriteLine($"{Tag} {message}");
        Debug.WriteLine($"{Tag} {message}");
    }
}
