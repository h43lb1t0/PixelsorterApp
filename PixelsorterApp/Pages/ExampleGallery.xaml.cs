using PixelsorterApp.Views;

namespace PixelsorterApp.Pages;

public partial class ExampleGallery : ContentPage
{
	public ExampleGallery()
	{
		InitializeComponent();

		var comparisonView = this.FindByName<BeforeAfterView>("comparisonView");
		if (comparisonView is null)
		{
			return;
		}

		comparisonView.BeforeImageSource = ImageSource.FromFile("ExampleImages/before_1.jpg");
		comparisonView.AfterImageSource = ImageSource.FromFile("ExampleImages/after_1.png");
	} 
}
