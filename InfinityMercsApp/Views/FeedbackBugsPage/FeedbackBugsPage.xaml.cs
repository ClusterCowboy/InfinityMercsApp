using InfinityMercsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views;

public partial class FeedbackBugsPage : ContentPage
{
    private readonly IFeedbackService? _feedbackService;

    public FeedbackBugsPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _feedbackService = services?.GetService<IFeedbackService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (CategoryPicker.SelectedIndex < 0)
        {
            CategoryPicker.SelectedIndex = 0;
        }
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (_feedbackService is null)
        {
            SetStatus("Feedback service unavailable.", isError: true);
            return;
        }

        var category = CategoryPicker.SelectedItem?.ToString() ?? string.Empty;
        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        var description = DescriptionEditor.Text?.Trim() ?? string.Empty;
        var userEmail = UserEmailEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(category))
        {
            SetStatus("Please choose a category.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            SetStatus("Title is required.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            SetStatus("Description is required.", isError: true);
            return;
        }

        if (!IsValidEmail(userEmail))
        {
            SetStatus("A valid email is required.", isError: true);
            return;
        }

        try
        {
            SetBusy(true);
            SetStatus(string.Empty);

            var result = await _feedbackService.SubmitAsync(new FeedbackSubmission
            {
                Category = category,
                Title = title,
                Description = description,
                UserEmail = userEmail
            });

            SetStatus(result.Message, isError: !result.Success);

            if (result.Success)
            {
                TitleEntry.Text = string.Empty;
                DescriptionEditor.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Submit failed: {ex.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        SubmitButton.IsEnabled = !isBusy;
        SubmitIndicator.IsVisible = isBusy;
        SubmitIndicator.IsRunning = isBusy;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.OrangeRed : Colors.LightGreen;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant);
    }
}
