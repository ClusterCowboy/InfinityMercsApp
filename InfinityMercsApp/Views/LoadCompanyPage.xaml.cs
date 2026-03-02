using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class LoadCompanyPage : ContentPage
{
    private const string SaveDirectoryName = "MercenaryRecords";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly SemaphoreSlim _loadRecordsGate = new(1, 1);
    private readonly SemaphoreSlim _iconLoadGate = new(1, 1);
    private SKPicture? _trashIconPicture;
    private readonly Dictionary<string, SKPicture?> _iconPictureCache = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<LoadCompanyRecordItem> SavedRecords { get; } = [];
    public bool HasSavedRecords => SavedRecords.Count > 0;
    public bool ShowEmptyState => !HasSavedRecords;

    public LoadCompanyPage()
    {
        InitializeComponent();
        BindingContext = this;
        SavedRecords.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSavedRecords));
            OnPropertyChanged(nameof(ShowEmptyState));
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTrashIconAsync();
        await LoadSavedRecordsAsync();
    }

    private async Task LoadTrashIconAsync()
    {
        if (_trashIconPicture is not null)
        {
            return;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-trash-1523235.svg");
            var svg = new SKSvg();
            _trashIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadCompanyPage trash icon load failed: {ex.Message}");
            _trashIconPicture = null;
        }
    }

    private async Task LoadSavedRecordsAsync()
    {
        await _loadRecordsGate.WaitAsync();
        try
        {
            SavedRecords.Clear();
            var saveDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, SaveDirectoryName);
            if (!Directory.Exists(saveDirectory))
            {
                return;
            }

            var items = new List<LoadCompanyRecordItem>();
            foreach (var filePath in Directory.EnumerateFiles(saveDirectory, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var payload = JsonSerializer.Deserialize<SavedCompanyFile>(json, JsonOptions);
                    if (payload is null)
                    {
                        continue;
                    }

                    var name = string.IsNullOrWhiteSpace(payload.CompanyName)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : payload.CompanyName;
                    var createdUtc = DateTimeOffset.TryParse(
                        payload.CreatedUtc,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var parsedCreatedUtc)
                        ? parsedCreatedUtc
                        : File.GetLastWriteTimeUtc(filePath);

                    items.Add(new LoadCompanyRecordItem
                    {
                        FilePath = filePath,
                        CompanyName = name,
                        CompanyType = payload.CompanyType,
                        SourceFactionIds = ResolveSourceFactionIds(payload),
                        SortDateUtc = createdUtc,
                        DisplayName = name,
                        Subtitle = $"{createdUtc:yyyy-MM-dd HH:mm:ss} UTC"
                    });
                }
                catch
                {
                    // Ignore malformed records and continue.
                }
            }

            await EnsureRecordIconsLoadedAsync(items);

            foreach (var item in items.OrderByDescending(x => x.SortDateUtc))
            {
                SavedRecords.Add(item);
            }
        }
        finally
        {
            _loadRecordsGate.Release();
        }
    }

    private async void OnDeleteRecordTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LoadCompanyRecordItem item)
        {
            return;
        }

        var shouldDelete = await DeleteCompanyConfirmationPage.ShowAsync(Navigation, item.CompanyName);
        if (!shouldDelete)
        {
            return;
        }

        try
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }

            await LoadSavedRecordsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Delete Failed", ex.Message, "OK");
        }
    }

    private async void OnLoadRecordTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable || bindable.BindingContext is not LoadCompanyRecordItem item)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.FilePath))
        {
            return;
        }

        var encodedPath = Uri.EscapeDataString(item.FilePath);
        await Shell.Current.GoToAsync($"{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
    }

    private void OnTrashIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_trashIconPicture is null)
        {
            return;
        }

        var bounds = _trashIconPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_trashIconPicture);
    }

    private void OnCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (sender is not SKCanvasView canvasView || canvasView.BindingContext is not LoadCompanyRecordItem item)
        {
            return;
        }

        var companyIconPath = GetCompanyTypeIconPath(item.CompanyType);
        var picture = TryGetLoadedPicture(companyIconPath);
        if (picture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, picture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private void OnFactionIconsPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (sender is not SKCanvasView canvasView || canvasView.BindingContext is not LoadCompanyRecordItem item)
        {
            return;
        }

        var factionIconPaths = item.SourceFactionIds
            .Distinct()
            .Take(2)
            .Select(GetFactionIconPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (factionIconPaths.Count == 0)
        {
            return;
        }

        if (factionIconPaths.Count == 1)
        {
            var picture = TryGetLoadedPicture(factionIconPaths[0]);
            if (picture is null)
            {
                return;
            }

            DrawPictureInRect(canvas, picture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
            return;
        }

        var gap = 4f;
        var totalWidth = e.Info.Width - gap;
        if (totalWidth <= 0)
        {
            return;
        }

        var slotWidth = totalWidth / 2f;
        var leftRect = new SKRect(0, 0, slotWidth, e.Info.Height);
        var rightRect = new SKRect(slotWidth + gap, 0, e.Info.Width, e.Info.Height);

        var leftPicture = TryGetLoadedPicture(factionIconPaths[0]);
        if (leftPicture is not null)
        {
            DrawPictureInRect(canvas, leftPicture, leftRect);
        }

        var rightPicture = TryGetLoadedPicture(factionIconPaths[1]);
        if (rightPicture is not null)
        {
            DrawPictureInRect(canvas, rightPicture, rightRect);
        }
    }

    private async Task EnsureRecordIconsLoadedAsync(IEnumerable<LoadCompanyRecordItem> items)
    {
        var iconPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            iconPaths.Add(GetCompanyTypeIconPath(item.CompanyType));

            foreach (var factionId in item.SourceFactionIds.Distinct().Take(2))
            {
                iconPaths.Add(GetFactionIconPath(factionId));
            }
        }

        foreach (var iconPath in iconPaths)
        {
            if (string.IsNullOrWhiteSpace(iconPath))
            {
                continue;
            }

            await LoadPictureAsync(iconPath);
        }
    }

    private async Task LoadPictureAsync(string relativePath)
    {
        await _iconLoadGate.WaitAsync();
        try
        {
            if (_iconPictureCache.ContainsKey(relativePath))
            {
                return;
            }

            SKPicture? picture = null;
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(relativePath);
                var svg = new SKSvg();
                picture = svg.Load(stream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LoadCompanyPage icon load failed for '{relativePath}': {ex.Message}");
            }

            _iconPictureCache[relativePath] = picture;
        }
        finally
        {
            _iconLoadGate.Release();
        }
    }

    private SKPicture? TryGetLoadedPicture(string relativePath)
    {
        if (_iconPictureCache.TryGetValue(relativePath, out var picture))
        {
            return picture;
        }

        return null;
    }

    private static string GetCompanyTypeIconPath(string? companyType)
    {
        return companyType switch
        {
            "Cohesive Company" => "SVGCache/MercsIcons/noun-team-7662436.svg",
            "Inspiring Leader" => "SVGCache/MercsIcons/noun-leadership-7195245.svg",
            "Airborne Company" => "SVGCache/MercsIcons/noun-airborne-8005870.svg",
            "TAG Company" => "SVGCache/MercsIcons/noun-battle-mech-1731140.svg",
            "Proxy Pack" => "SVGCache/MercsIcons/noun-assassin-5981200.svg",
            _ => "SVGCache/MercsIcons/noun-hack-2277937.svg"
        };
    }

    private static string GetFactionIconPath(int factionId)
    {
        return $"SVGCache/factions/{factionId}.svg";
    }

    private static List<int> ResolveSourceFactionIds(SavedCompanyFile payload)
    {
        var sourceFactionIds = payload.SourceFactions
            .Select(x => x.FactionId)
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (sourceFactionIds.Count > 0)
        {
            return sourceFactionIds;
        }

        return payload.Entries
            .Select(x => x.SourceFactionId)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
    }

    private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var drawnWidth = bounds.Width * scale;
        var drawnHeight = bounds.Height * scale;
        var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
        var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }
}

public sealed class LoadCompanyRecordItem
{
    public string FilePath { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public List<int> SourceFactionIds { get; init; } = [];
    public DateTimeOffset SortDateUtc { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
}

public sealed class DeleteCompanyConfirmationPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private DeleteCompanyConfirmationPage(string companyName)
    {
        BackgroundColor = Color.FromRgba(0, 0, 0, 180);

        var backButton = new Button
        {
            Text = "BACK",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            Command = new Command(async () => await CloseAsync(false))
        };
        var confirmButton = new Button
        {
            Text = "CONFIRM",
            BackgroundColor = Color.FromArgb("#DC2626"),
            TextColor = Colors.Black,
            Command = new Command(async () => await CloseAsync(true))
        };

        var buttonsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        buttonsGrid.Add(backButton);
        buttonsGrid.Add(confirmButton);
        Grid.SetColumn(backButton, 0);
        Grid.SetColumn(confirmButton, 1);

        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#111827"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                WidthRequest = 340,
                Children =
                {
                    new Label
                    {
                        Text = $"Confirm deletion of company [{companyName}]",
                        FontSize = 18,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    buttonsGrid
                }
            }
        };

        Content = new Grid
        {
            Children = { card }
        };
    }

    public static async Task<bool> ShowAsync(INavigation navigation, string companyName)
    {
        var page = new DeleteCompanyConfirmationPage(companyName);
        await navigation.PushModalAsync(page, false);
        return await page._resultSource.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(false);
        return true;
    }

    private async Task CloseAsync(bool confirmed)
    {
        if (!_resultSource.TrySetResult(confirmed))
        {
            return;
        }

        await Navigation.PopModalAsync(false);
    }
}
