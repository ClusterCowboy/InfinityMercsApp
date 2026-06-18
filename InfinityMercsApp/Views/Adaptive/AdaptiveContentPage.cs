namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// Base page that tracks the active <see cref="AdaptiveLayoutMode"/> from the page's allocated
/// width and exposes it for XAML bindings and code-behind reflow. See
/// <c>Docs/AdaptiveLayoutDefinitions.md</c> for the intended per-page layouts.
/// </summary>
/// <remarks>
/// Inherits <see cref="ContentPage"/> directly (not <c>ContentPageBase</c>) so that subclasses keep
/// full control over their own <c>OnAppearing</c> lifecycle. Convenience getters raise
/// <see cref="BindableObject.OnPropertyChanged"/> so bindings such as
/// <c>{Binding Source={x:Reference Root}, Path=IsCompact}</c> re-evaluate on every mode change.
/// </remarks>
public abstract class AdaptiveContentPage : ContentPage
{
    // Breakpoints (logical units of page width) from AdaptiveLayoutDefinitions.md.
    private const double MediumMinWidth = 600d;
    private const double ExpandedMinWidth = 900d;
    private const double WideMinWidth = 1200d;

    private AdaptiveLayoutMode _layoutMode = AdaptiveLayoutMode.Compact;

    /// <summary>The active layout mode for the current page width.</summary>
    public AdaptiveLayoutMode LayoutMode
    {
        get => _layoutMode;
        private set
        {
            if (_layoutMode == value)
            {
                return;
            }

            _layoutMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompact));
            OnPropertyChanged(nameof(IsMedium));
            OnPropertyChanged(nameof(IsExpanded));
            OnPropertyChanged(nameof(IsWide));
            OnPropertyChanged(nameof(IsMediumOrWider));
            OnPropertyChanged(nameof(IsExpandedOrWider));
            OnLayoutModeChanged(value);
        }
    }

    public bool IsCompact => _layoutMode == AdaptiveLayoutMode.Compact;
    public bool IsMedium => _layoutMode == AdaptiveLayoutMode.Medium;
    public bool IsExpanded => _layoutMode == AdaptiveLayoutMode.Expanded;
    public bool IsWide => _layoutMode == AdaptiveLayoutMode.Wide;
    public bool IsMediumOrWider => _layoutMode >= AdaptiveLayoutMode.Medium;
    public bool IsExpandedOrWider => _layoutMode >= AdaptiveLayoutMode.Expanded;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0)
        {
            return;
        }

        LayoutMode = ResolveMode(width);
    }

    /// <summary>
    /// Override to rebuild structure that cannot be data-bound (Grid row/column definitions,
    /// re-parenting panes). Called only when the mode actually changes.
    /// </summary>
    protected virtual void OnLayoutModeChanged(AdaptiveLayoutMode mode)
    {
    }

    private static AdaptiveLayoutMode ResolveMode(double width) => width switch
    {
        >= WideMinWidth => AdaptiveLayoutMode.Wide,
        >= ExpandedMinWidth => AdaptiveLayoutMode.Expanded,
        >= MediumMinWidth => AdaptiveLayoutMode.Medium,
        _ => AdaptiveLayoutMode.Compact
    };
}
