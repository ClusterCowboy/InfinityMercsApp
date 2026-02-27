using System.Collections.ObjectModel;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

public class ViewerViewModel : BaseViewModel
{
    private readonly IMetadataAccessor? _metadataAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private bool _isLoading;
    private string _status = "Loading factions...";
    private ViewerFactionItem? _selectedFaction;

    public ViewerViewModel(
        IMetadataAccessor? metadataAccessor = null,
        FactionLogoCacheService? factionLogoCacheService = null)
    {
        _metadataAccessor = metadataAccessor;
        _factionLogoCacheService = factionLogoCacheService;
        SelectFactionCommand = new Command<ViewerFactionItem>(item =>
        {
            if (item is not null)
            {
                SelectedFaction = item;
            }
        });
    }

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public ViewerFactionItem? SelectedFaction
    {
        get => _selectedFaction;
        set
        {
            if (_selectedFaction == value)
            {
                return;
            }

            _selectedFaction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFactionLogoUrl));
        }
    }

    public string SelectedFactionLogoUrl => SelectedFaction?.Logo ?? string.Empty;

    public ICommand SelectFactionCommand { get; }

    public async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        if (_metadataAccessor is null)
        {
            Status = "Metadata service unavailable.";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = await _metadataAccessor.GetFactionsAsync(false, cancellationToken);

            Factions.Clear();
            foreach (var faction in factions)
            {
                Factions.Add(new ViewerFactionItem
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Logo = faction.Logo,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id)
                });
            }

            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadFactionsAsync failed: {ex.Message}");
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class ViewerFactionItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }
}
