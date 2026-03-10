using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Views;

public partial class CreateNewArmyPage
{
    public CreateNewArmyPage(CreateNewArmyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
