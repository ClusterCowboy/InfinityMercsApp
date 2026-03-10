using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class AppSettingsProvider(ISQLiteRepository sqliteRepository) : IAppSettingsProvider
{
    private const string DisplayUnitsKey = "display_units";
    private const string DisplayUnitsInches = "inches";
    private const string DisplayUnitsCentimeters = "centimeters";
    private const string FeedbackApiEndpointKey = "feedback_api_endpoint";

    /// <inheritdoc/>
    public bool GetShowUnitsInInches()
    {

        var setting = sqliteRepository.GetAll<AppSetting>(x => x.Key == DisplayUnitsKey).FirstOrDefault();

        if (setting is null)
        {
            return true;
        }

        return !string.Equals(setting.Value, DisplayUnitsCentimeters, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void SetShowUnitsInInches(bool showUnitsInInches)
    {
        var value = showUnitsInInches ? DisplayUnitsInches : DisplayUnitsCentimeters;
        var existing = sqliteRepository.GetAll<AppSetting>(x => x.Key == DisplayUnitsKey).FirstOrDefault();

        if (existing is null)
        {
            sqliteRepository.Insert([new AppSetting
            {
                Key = DisplayUnitsKey,
                Value = value
            }]);
            return;
        }

        existing.Value = value;
        sqliteRepository.Update(existing);
    }

    /// <inheritdoc/>
    public string GetFeedbackApiEndpoint()
    {
        var setting = sqliteRepository.GetAll<AppSetting>(x => x.Key == FeedbackApiEndpointKey).FirstOrDefault();

        return setting?.Value?.Trim() ?? string.Empty;
    }

    /// <inheritdoc/>
    public void SetFeedbackApiEndpoint(string endpoint)
    {
        var sanitizedValue = endpoint?.Trim() ?? string.Empty;
        var existing = sqliteRepository.GetAll<AppSetting>(x => x.Key == FeedbackApiEndpointKey).FirstOrDefault();

        if (existing is null)
        {
            sqliteRepository.Insert([new AppSetting
            {
                Key = FeedbackApiEndpointKey,
                Value = sanitizedValue
            }]);
            return;
        }

        existing.Value = sanitizedValue;
        sqliteRepository.Update(existing);
    }
}

