using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Globalization;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class AppSettingsProvider(ISQLiteRepository sqliteRepository) : IAppSettingsProvider
{
    private const string DisplayUnitsKey = "display_units";
    private const string DisplayUnitsInches = "inches";
    private const string DisplayUnitsCentimeters = "centimeters";
    private const string FeedbackApiEndpointKey = "feedback_api_endpoint";
    private const string StartupUpdateLastAttemptUtcKey = "startup_update_last_attempt_utc";

    /// <inheritdoc/>
    public bool GetShowUnitsInInches()
    {
        var setting = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == DisplayUnitsKey);

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
        var existing = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == DisplayUnitsKey);

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
        var setting = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == FeedbackApiEndpointKey);

        return setting?.Value?.Trim() ?? string.Empty;
    }

    /// <inheritdoc/>
    public void SetFeedbackApiEndpoint(string endpoint)
    {
        var sanitizedValue = endpoint?.Trim() ?? string.Empty;
        var existing = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == FeedbackApiEndpointKey);

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

    /// <inheritdoc/>
    public DateTimeOffset? GetStartupUpdateLastAttemptUtc()
    {
        var setting = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == StartupUpdateLastAttemptUtcKey);

        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                setting.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var lastAttemptUtc))
        {
            return null;
        }

        return lastAttemptUtc;
    }

    /// <inheritdoc/>
    public void SetStartupUpdateLastAttemptUtc(DateTimeOffset attemptedAtUtc)
    {
        var value = attemptedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        var existing = sqliteRepository.FirstOrDefault<AppSetting>(x => x.Key == StartupUpdateLastAttemptUtcKey);

        if (existing is null)
        {
            sqliteRepository.Insert([new AppSetting
            {
                Key = StartupUpdateLastAttemptUtcKey,
                Value = value
            }]);
            return;
        }

        existing.Value = value;
        sqliteRepository.Update(existing);
    }
}
