using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Models;

namespace InfinityMercsApp.Services;

public class AppSettingsService
{
    private const string DisplayUnitsKey = "display_units";
    private const string DisplayUnitsInches = "inches";
    private const string DisplayUnitsCentimeters = "centimeters";
    private const string FeedbackApiEndpointKey = "feedback_api_endpoint";

    private readonly IDatabaseContext _databaseContext;

    public AppSettingsService(IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task<bool> GetShowUnitsInInchesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var setting = await _databaseContext.Connection.Table<AppSetting>()
            .Where(x => x.Key == DisplayUnitsKey)
            .FirstOrDefaultAsync();

        if (setting is null)
        {
            return true;
        }

        return !string.Equals(setting.Value, DisplayUnitsCentimeters, StringComparison.OrdinalIgnoreCase);
    }

    public async Task SetShowUnitsInInchesAsync(bool showUnitsInInches, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var value = showUnitsInInches ? DisplayUnitsInches : DisplayUnitsCentimeters;
        var existing = await _databaseContext.Connection.Table<AppSetting>()
            .Where(x => x.Key == DisplayUnitsKey)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _databaseContext.Connection.InsertAsync(new AppSetting
            {
                Key = DisplayUnitsKey,
                Value = value
            });
            return;
        }

        existing.Value = value;
        await _databaseContext.Connection.UpdateAsync(existing);
    }

    public async Task<string> GetFeedbackApiEndpointAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var setting = await _databaseContext.Connection.Table<AppSetting>()
            .Where(x => x.Key == FeedbackApiEndpointKey)
            .FirstOrDefaultAsync();

        return setting?.Value?.Trim() ?? string.Empty;
    }

    public async Task SetFeedbackApiEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var sanitizedValue = endpoint?.Trim() ?? string.Empty;
        var existing = await _databaseContext.Connection.Table<AppSetting>()
            .Where(x => x.Key == FeedbackApiEndpointKey)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            await _databaseContext.Connection.InsertAsync(new AppSetting
            {
                Key = FeedbackApiEndpointKey,
                Value = sanitizedValue
            });
            return;
        }

        existing.Value = sanitizedValue;
        await _databaseContext.Connection.UpdateAsync(existing);
    }
}
