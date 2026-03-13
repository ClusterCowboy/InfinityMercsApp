using InfinityMercsApp.Infrastructure.Providers;

namespace InfinityMercsApp.Services;

public class AppSettingsService
{
    private readonly IAppSettingsProvider _appSettingsProvider;

    public AppSettingsService(IAppSettingsProvider appSettingsProvider)
    {
        _appSettingsProvider = appSettingsProvider;
    }

    public async Task<bool> GetShowUnitsInInchesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(_appSettingsProvider.GetShowUnitsInInches());
    }

    public async Task SetShowUnitsInInchesAsync(bool showUnitsInInches, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _appSettingsProvider.SetShowUnitsInInches(showUnitsInInches);
        await Task.CompletedTask;
    }

    public async Task<string> GetFeedbackApiEndpointAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(_appSettingsProvider.GetFeedbackApiEndpoint());
    }

    public async Task SetFeedbackApiEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _appSettingsProvider.SetFeedbackApiEndpoint(endpoint);
        await Task.CompletedTask;
    }
}
