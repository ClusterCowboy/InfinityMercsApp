using InfinityMercsApp.Views.Common;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Compact-screen header button that shows the icon(s) of the currently chosen faction slot(s) and,
/// when tapped, reopens the faction selector. Mirrors the title-bar <see cref="FactionSlotSelectorView"/>
/// but is sized for the top of the page content where the cramped title bar is hidden on phones.
/// The hosting page keeps it in sync by calling <see cref="SetSlot"/> from its slot-icon load path.
/// </summary>
public partial class SelectedFactionButtonView : ContentView
{
    private SKPicture? _leftPicture;
    private SKPicture? _rightPicture;

    public static readonly BindableProperty ShowRightSlotProperty =
        BindableProperty.Create(nameof(ShowRightSlot), typeof(bool), typeof(SelectedFactionButtonView), false);

    public event EventHandler? Tapped;

    public SelectedFactionButtonView()
    {
        InitializeComponent();
    }

    public bool ShowRightSlot
    {
        get => (bool)GetValue(ShowRightSlotProperty);
        set => SetValue(ShowRightSlotProperty, value);
    }

    /// <summary>Loads and shows the faction logo for the given slot (0 = left, 1 = right).</summary>
    public void SetSlot(int slotIndex, string? cachedPath, string? packagedPath)
    {
        _ = SetSlotAsync(slotIndex, cachedPath, packagedPath);
    }

    private async Task SetSlotAsync(int slotIndex, string? cachedPath, string? packagedPath)
    {
        var picture = await LoadPictureAsync(cachedPath, packagedPath);

        if (slotIndex == 0)
        {
            _leftPicture?.Dispose();
            _leftPicture = picture;
            LeftSlotCanvas.InvalidateSurface();
        }
        else
        {
            _rightPicture?.Dispose();
            _rightPicture = picture;
            RightSlotCanvas.InvalidateSurface();
        }

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        var hasSelection = _leftPicture is not null || _rightPicture is not null;
        PromptLabel.Text = hasSelection ? "" : "Faction";
    }

    private static async Task<SKPicture?> LoadPictureAsync(string? cachedPath, string? packagedPath)
    {
        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                stream = File.OpenRead(cachedPath);
            }
            else if (!string.IsNullOrWhiteSpace(packagedPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            }

            if (stream is null)
            {
                return null;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                return svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SelectedFactionButtonView icon load failed: {ex.Message}");
            return null;
        }
    }

    private void OnLeftSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionSharedUtilities.DrawSlotPicture(_leftPicture, e);
    }

    private void OnRightSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionSharedUtilities.DrawSlotPicture(_rightPicture, e);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        Tapped?.Invoke(this, EventArgs.Empty);
    }
}
