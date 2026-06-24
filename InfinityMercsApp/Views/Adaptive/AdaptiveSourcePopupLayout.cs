namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// Shared adaptive reflow for the company source-selection popups (Standard / TAG / Lone Wolf),
/// which are structurally identical: a centered modal card holding a two-up choice grid.
/// Compact stacks the choices vertically and lets the card fill the screen; medium and wider keep
/// them side-by-side inside a centered, width-capped modal. See
/// <c>Docs/AdaptiveLayoutDefinitions.md</c>.
/// </summary>
public static class AdaptiveSourcePopupLayout
{
    public static void Apply(AdaptiveContentPage page, Border modalCard, Grid cardsGrid, View cardOne, View cardTwo)
    {
        if (page.IsCompact)
        {
            modalCard.HorizontalOptions = LayoutOptions.Fill;
            modalCard.VerticalOptions = LayoutOptions.Fill;
            modalCard.MaximumWidthRequest = double.PositiveInfinity;

            cardsGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            cardsGrid.RowDefinitions =
            [
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            ];
            cardsGrid.ColumnSpacing = 0;
            cardsGrid.RowSpacing = 16;

            Grid.SetColumn(cardOne, 0);
            Grid.SetRow(cardOne, 0);
            Grid.SetColumn(cardTwo, 0);
            Grid.SetRow(cardTwo, 1);
        }
        else
        {
            modalCard.HorizontalOptions = LayoutOptions.Center;
            modalCard.VerticalOptions = LayoutOptions.Center;
            modalCard.MaximumWidthRequest = page.IsMedium ? 700d : 820d;

            cardsGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
            cardsGrid.ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            ];
            cardsGrid.RowSpacing = 0;
            cardsGrid.ColumnSpacing = 16;

            Grid.SetRow(cardOne, 0);
            Grid.SetColumn(cardOne, 0);
            Grid.SetRow(cardTwo, 0);
            Grid.SetColumn(cardTwo, 1);
        }
    }
}
