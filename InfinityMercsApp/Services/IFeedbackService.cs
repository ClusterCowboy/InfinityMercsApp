namespace InfinityMercsApp.Services;

/// <summary>
/// A service for feedback on the application.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Submits feedback.
    /// </summary>
    /// <param name="submission"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<FeedbackSubmitResult> SubmitAsync(FeedbackSubmission submission, CancellationToken cancellationToken = default);
}
