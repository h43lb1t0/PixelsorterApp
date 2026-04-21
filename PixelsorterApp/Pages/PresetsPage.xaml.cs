namespace PixelsorterApp.Pages;

public partial class PresetsPage : ContentPage
{
   public PresetsPage(ViewModels.PresetsPageViewModel viewModel)
	{
		InitializeComponent();
       BindingContext = viewModel;
	}
}