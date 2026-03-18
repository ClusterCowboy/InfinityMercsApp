using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Reusable title-bar control for season start points and company-validity indicator.
/// </summary>
public partial class SeasonStartPointsView : ContentView
{
    private SKPicture? _validMercsListIcon;
    private SKPicture? _invalidMercsListIcon;
    private bool _showSeasonCheckIcon;
    private bool _isCompanyValid;
    private string _pointsRemainingText = "75";

    public static readonly BindableProperty SeasonPointsCapTextProperty =
        BindableProperty.Create(nameof(SeasonPointsCapText), typeof(string), typeof(SeasonStartPointsView), "0", propertyChanged: OnSeasonPointsCapTextChanged);

    public static readonly BindableProperty SelectedStartSeasonPointsProperty =
        BindableProperty.Create(nameof(SelectedStartSeasonPoints), typeof(string), typeof(SeasonStartPointsView), "75", BindingMode.TwoWay, propertyChanged: OnSelectedStartSeasonPointsChanged);

    public static readonly BindableProperty IsCompanyValidProperty =
        BindableProperty.Create(
            nameof(IsCompanyValid),
            typeof(bool),
            typeof(SeasonStartPointsView),
            false,
            propertyChanged: OnIsCompanyValidChanged);

    public SeasonStartPointsView()
    {
        InitializeComponent();
        _ = LoadSeasonValidationIconsAsync();
    }

    /// <summary>
    /// Current merc list total shown to the left of the season limit picker.
    /// </summary>
    public string SeasonPointsCapText
    {
        get => (string)GetValue(SeasonPointsCapTextProperty);
        set => SetValue(SeasonPointsCapTextProperty, value);
    }

    /// <summary>
    /// Selected season-start point cap.
    /// </summary>
    public string SelectedStartSeasonPoints
    {
        get => (string)GetValue(SelectedStartSeasonPointsProperty);
        set => SetValue(SelectedStartSeasonPointsProperty, value);
    }

    /// <summary>
    /// Points remaining (limit minus current cost), formatted as a string for display.
    /// </summary>
    public string PointsRemainingText
    {
        get => _pointsRemainingText;
        private set
        {
            if (_pointsRemainingText == value) return;
            _pointsRemainingText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether the current company is valid for start; drives the check/X icon.
    /// </summary>
    public bool IsCompanyValid
    {
        get => _isCompanyValid;
        set => SetValue(IsCompanyValidProperty, value);
    }

    public event EventHandler? SelectedStartSeasonPointsChanged;

    private static void OnSelectedStartSeasonPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SeasonStartPointsView view) return;
        view.UpdatePointsRemaining();
        view.SelectedStartSeasonPointsChanged?.Invoke(view, EventArgs.Empty);
    }

    private static void OnSeasonPointsCapTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SeasonStartPointsView view)
            view.UpdatePointsRemaining();
    }

    private void UpdatePointsRemaining()
    {
        var limit = int.TryParse(SelectedStartSeasonPoints, out var l) ? l : 0;
        var current = int.TryParse(SeasonPointsCapText, out var c) ? c : 0;
        PointsRemainingText = (limit - current).ToString();
    }

    private static void OnIsCompanyValidChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SeasonStartPointsView view || newValue is not bool isValid)
        {
            return;
        }

        view._isCompanyValid = isValid;
        view._showSeasonCheckIcon = isValid;
        view.SeasonValidationCanvas.InvalidateSurface();
    }

    private async Task LoadSeasonValidationIconsAsync()
    {
        _validMercsListIcon?.Dispose();
        _validMercsListIcon = null;
        _invalidMercsListIcon?.Dispose();
        _invalidMercsListIcon = null;

        try
        {
            await using var checkStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var checkSvg = new SKSvg();
            _validMercsListIcon = checkSvg.Load(checkStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonStartPointsView season check icon load failed: {ex.Message}");
        }

        try
        {
            await using var xStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var xSvg = new SKSvg();
            _invalidMercsListIcon = xSvg.Load(xStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonStartPointsView season x icon load failed: {ex.Message}");
        }

        SeasonValidationCanvas.InvalidateSurface();
    }

    private void OnSeasonValidationCanvasPaintSurface(object? sender, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        var icon = _showSeasonCheckIcon ? _validMercsListIcon : _invalidMercsListIcon;
        DrawSlotPicture(icon, e);
    }

    private static void DrawSlotPicture(SKPicture? picture, SkiaSharp.Views.Maui.SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }
}
