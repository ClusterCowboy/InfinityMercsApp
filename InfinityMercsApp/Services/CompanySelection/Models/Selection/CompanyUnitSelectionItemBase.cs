using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Common;

public abstract class CompanyUnitSelectionItemBase : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }
    public int SourceFactionId { get; init; }
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }
    public string? Slug { get; init; }

    public int? Type { get; init; }
    public bool IsCharacter { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }
    public bool IsSpecOps { get; init; }
    public bool UseBlueHalfOpacityBackground { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public string UnitListBackgroundColor => IsSelected
        ? "#334155"
        : UseBlueHalfOpacityBackground
            ? "#800000FF"
            : "Transparent";

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnitListBackgroundColor));
        }
    }
}
