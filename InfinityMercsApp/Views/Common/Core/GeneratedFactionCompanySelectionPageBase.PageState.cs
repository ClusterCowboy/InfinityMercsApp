using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected string _companyName = "Company Name";
    protected Command _startCompanyCommand = null!;
    protected bool _showCompanyNameValidationError;
    protected Color _companyNameBorderColor = Color.FromArgb("#8A97A8");
    protected bool _loaded;

    public string SelectedStartSeasonPoints
    {
        get => SeasonStartPointsControl.SelectedStartSeasonPoints;
        set
        {
            if (SeasonStartPointsControl.SelectedStartSeasonPoints == value)
            {
                return;
            }

            SeasonStartPointsControl.SelectedStartSeasonPoints = value;
            OnPropertyChanged();
        }
    }

    protected void OnSelectedStartSeasonPointsChanged(object? sender, EventArgs e)
    {
        UpdateSeasonValidationState();
        _ = RefreshSeasonPointsDependentUnitStateAsync();
    }

    public string SeasonPointsCapText
    {
        get => SeasonStartPointsControl.SeasonPointsCapText;
        set
        {
            if (SeasonStartPointsControl.SeasonPointsCapText == value)
            {
                return;
            }

            SeasonStartPointsControl.SeasonPointsCapText = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string CompanyName
    {
        get => _companyName;
        set
        {
            if (_companyName == value)
            {
                return;
            }

            _companyName = value;
            OnPropertyChanged();
            if (_showCompanyNameValidationError && CompanyStartSharedState.IsCompanyNameValid(value))
            {
                SetCompanyNameValidationError(false);
            }
        }
    }

    public bool IsCompanyValid
    {
        get => SeasonStartPointsControl.IsCompanyValid;
        private set
        {
            if (SeasonStartPointsControl.IsCompanyValid == value)
            {
                return;
            }

            SeasonStartPointsControl.IsCompanyValid = value;
            OnPropertyChanged();
            _startCompanyCommand.ChangeCanExecute();
        }
    }

    public bool ShowCompanyNameValidationError
    {
        get => _showCompanyNameValidationError;
        protected set
        {
            if (_showCompanyNameValidationError == value)
            {
                return;
            }

            _showCompanyNameValidationError = value;
            OnPropertyChanged();
        }
    }

    public Color CompanyNameBorderColor
    {
        get => _companyNameBorderColor;
        protected set
        {
            if (_companyNameBorderColor == value)
            {
                return;
            }

            _companyNameBorderColor = value;
            OnPropertyChanged();
        }
    }

    protected override void ApplyLieutenantVisualStatesFromBase()
    {
        ApplyLieutenantVisualStates();
    }

    protected override Task ApplyUnitVisibilityFiltersFromBaseAsync()
    {
        return ApplyUnitVisibilityFiltersAsync();
    }

    protected override Task LoadFactionsAsync()
    {
        return LoadFactionsAsync(CancellationToken.None);
    }

    protected override Task LoadUnitsForActiveSlotAsync()
    {
        return LoadUnitsForActiveSlotAsync(CancellationToken.None);
    }

    protected override Task LoadSelectedUnitDetailsAsync()
    {
        return LoadSelectedUnitDetailsAsync(CancellationToken.None);
    }

    private async Task RefreshSeasonPointsDependentUnitStateAsync(CancellationToken cancellationToken = default)
    {
        _filterState.PreparedUnitFilterPopupOptions = null;
        await ApplyUnitVisibilityFiltersAsync(cancellationToken);
        await BuildUnitFilterPopupOptionsAsync(cancellationToken);
    }

    UnitFilterCriteria ICompanySelectionVisibilityState.ActiveUnitFilter => _filterState.ActiveUnitFilter;

    protected void SwitchToLeftSlot()
    {
        _activeSlotIndex = 0;
        FactionSlotSelectorViewForVisuals.ApplyActiveSlotBorders(0);
        _filterState.ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = true };
        LieutenantOnlyUnits = true;
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);
    }

    protected void SetActiveSlot(int index)
    {
        var previousSlot = _activeSlotIndex;
        _activeSlotIndex = ResolveActiveSlotIndexCore(index, true);
        FactionSlotSelectorViewForVisuals.ApplyActiveSlotBorders(_activeSlotIndex);

        var isLeftSlot = _activeSlotIndex == 0;
        _filterState.ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = isLeftSlot };
        LieutenantOnlyUnits = isLeftSlot;
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);

        if (previousSlot != _activeSlotIndex && _loaded)
        {
            _ = LoadUnitsForActiveSlotAsync();
        }
    }
}
