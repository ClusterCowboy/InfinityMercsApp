using System.Net.Http.Json;
using System.Text.Json;

namespace InfinityMercsApp.Services;

public interface IFeedbackService
{
    Task<FeedbackSubmitResult> SubmitAsync(FeedbackSubmission submission, CancellationToken cancellationToken = default);
}

public sealed class FeedbackService : IFeedbackService
{
    private const string FeedbackRecipientEmail = "jeremiahpatrick@protonmail.com";
    private static readonly Uri FeedbackEndpoint = new($"https://formsubmit.co/ajax/{FeedbackRecipientEmail}");

    private readonly HttpClient _httpClient;

    public FeedbackService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FeedbackSubmitResult> SubmitAsync(FeedbackSubmission submission, CancellationToken cancellationToken = default)
    {
        var payload = new FeedbackApiRequest
        {
            Name = "InfinityMercsApp",
            Email = FeedbackRecipientEmail,
            Subject = $"Infinity Mercs App Feedback/Bug - {submission.Title.Trim()}",
            Message = BuildMessage(submission),
            ReplyTo = submission.UserEmail.Trim(),
            Captcha = "false",
            Template = "table"
        };

        using var response = await _httpClient.PostAsJsonAsync(FeedbackEndpoint, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return FeedbackSubmitResult.Fail(
                    $"Ticket submit failed: backend returned HTML ({(int)response.StatusCode} {response.ReasonPhrase}). Verify the endpoint is your backend API, not a web page.");
            }

            if (string.IsNullOrWhiteSpace(errorBody))
            {
                errorBody = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            }

            return FeedbackSubmitResult.Fail($"Email send failed: {errorBody}");
        }

        FeedbackApiResponse? body = null;
        try
        {
            body = await response.Content.ReadFromJsonAsync<FeedbackApiResponse>(cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            // Optional response body format; if parsing fails but status is success, still treat as success.
        }

        return FeedbackSubmitResult.Ok(null);
    }

    private static string BuildMessage(FeedbackSubmission submission)
    {
        return string.Join(Environment.NewLine, [
            $"Category: {submission.Category.Trim()}",
            $"Title: {submission.Title.Trim()}",
            $"User Email: {submission.UserEmail.Trim()}",
            $"Submitted At (UTC): {DateTimeOffset.UtcNow:O}",
            $"App Version: {AppInfo.Current.VersionString}",
            $"Platform: {DeviceInfo.Current.Platform}",
            $"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
            $"Device Name: {DeviceInfo.Current.Name}",
            string.Empty,
            "Description:",
            submission.Description.Trim()
        ]);
    }
}

public sealed class FeedbackSubmission
{
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
}

public sealed class FeedbackSubmitResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? IssueUrl { get; init; }

    public static FeedbackSubmitResult Ok(string? issueUrl)
    {
        var message = string.IsNullOrWhiteSpace(issueUrl)
            ? "Feedback email sent successfully."
            : $"Feedback email sent successfully: {issueUrl}";

        return new FeedbackSubmitResult
        {
            Success = true,
            Message = message,
            IssueUrl = issueUrl
        };
    }

    public static FeedbackSubmitResult Fail(string message)
    {
        return new FeedbackSubmitResult
        {
            Success = false,
            Message = message
        };
    }
}

public sealed class FeedbackApiRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_replyto")]
    public string ReplyTo { get; init; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("_captcha")]
    public string Captcha { get; init; } = "false";

    [System.Text.Json.Serialization.JsonPropertyName("_template")]
    public string Template { get; init; } = "table";
}

public sealed class FeedbackApiResponse
{
    public string? IssueUrl { get; init; }
    public int? IssueNumber { get; init; }
}
