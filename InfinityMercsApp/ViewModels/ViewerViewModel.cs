using System.Collections.ObjectModel;
using InfinityMercsApp.Data.Database;

namespace InfinityMercsApp.ViewModels;

public class ViewerViewModel : BaseViewModel
{
    private readonly IMetadataAccessor? _metadataAccessor;
    private bool _isLoading;
    private string _status = "Loading factions...";

    public ViewerViewModel(IMetadataAccessor? metadataAccessor = null)
    {
        _metadataAccessor = metadataAccessor;
    }

    public ObservableCollection<FactionRecord> Factions { get; } = [];

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
                Factions.Add(faction);
            }

            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
