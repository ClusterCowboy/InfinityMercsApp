namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// Width-driven layout mode shared by adaptive pages. Thresholds are expressed in MAUI logical
/// units of available page width (not raw pixels and not window width), matching
/// <c>Docs/AdaptiveLayoutDefinitions.md</c>.
/// </summary>
public enum AdaptiveLayoutMode
{
    /// <summary>Phone-first, one column. Width &lt; 600.</summary>
    Compact,

    /// <summary>Tablet portrait or narrow desktop, usually two zones. Width 600-899.</summary>
    Medium,

    /// <summary>Tablet landscape or normal desktop, side-by-side work areas. Width 900-1199.</summary>
    Expanded,

    /// <summary>Large desktop, supporting rails and denser multi-column lists. Width &gt;= 1200.</summary>
    Wide
}
