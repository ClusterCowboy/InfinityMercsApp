using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class CreateNewArmyPage
{
    public CreateNewArmyPage(CreateNewArmyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
