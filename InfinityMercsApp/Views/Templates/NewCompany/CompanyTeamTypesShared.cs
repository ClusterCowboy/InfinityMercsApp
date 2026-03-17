using System.Collections.ObjectModel;
using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Templates.NewCompany;

public abstract class CompanyTeamListItemBase<TAllowedProfile> : BaseViewModel
    where TAllowedProfile : class
{
    public string Name { get; init; } = string.Empty;
    public string TeamCountsText { get; init; } = string.Empty;
    public bool HasTeamCounts => !string.IsNullOrWhiteSpace(TeamCountsText);
    public bool IsWildcardBucket { get; init; }
    public ObservableCollection<TAllowedProfile> AllowedProfiles { get; init; } = [];

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

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }
}

public abstract class CompanyTeamUnitLimitItemBase : BaseViewModel, IViewerListItem
{
    public string Name { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
    public string? Slug { get; init; }
    public bool IsCharacter { get; init; }
    public int? ResolvedUnitId { get; init; }
    public int? ResolvedSourceFactionId { get; init; }
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

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
        }
    }
}
