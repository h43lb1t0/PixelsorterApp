using System.ComponentModel;
using PixelsorterApp.ViewModels;

namespace PixelsorterApp.Views;

public partial class AngleArrowOverlayView : ContentView
{
    private MainPageViewModel? viewModel;

    public AngleArrowOverlayView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (viewModel is not null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = BindingContext as MainPageViewModel;
        if (viewModel is not null)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.ShowAngleOverlay))
        {
            _ = FadeAsync(viewModel?.ShowAngleOverlay == true);
        }
    }

    private async Task FadeAsync(bool show)
    {
        this.AbortAnimation("FadeTo");
        if (show)
        {
            IsVisible = true;
            await this.FadeToAsync(1, 120, Easing.CubicOut);
        }
        else
        {
            await this.FadeToAsync(0, 300, Easing.CubicIn);
            if (viewModel?.ShowAngleOverlay != true)
            {
                IsVisible = false;
            }
        }
    }
}