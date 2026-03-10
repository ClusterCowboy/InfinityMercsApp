namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// A provider to handle interfactions with AppSettings
/// </summary>
public interface IAppSettingsProvider
{
    /// <summary>
    /// Gets a value indicating whether units should be shown in inches.
    /// </summary>
    /// <returns></returns>
    public bool GetShowUnitsInInches();

    /// <summary>
    /// Sets a value indicating whether units should be shown in inches.
    /// </summary>
    /// <param name="showUnitsInInches"></param>
    public void SetShowUnitsInInches(bool showUnitsInInches);

    /// <summary>
    /// Gets a string representing the Feedback API endpoint.
    /// </summary>
    /// <returns></returns>
    public string GetFeedbackApiEndpoint();

    /// <summary>
    /// Sets the Feedback API endpoint.
    /// </summary>
    /// <param name="endpoint"></param>
    public void SetFeedbackApiEndpoint(string endpoint);
}
