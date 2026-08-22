using UXDivers.Popups;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

namespace PixelsorterApp.Popups;

public partial class ConfirmationPopup : PopupResultPage<bool>
{

    public ConfirmationPopup(bool closeOnBackgroundTap = false)
    {
        InitializeComponent();

        CloseWhenBackgroundIsClicked = closeOnBackgroundTap;


        if (Microsoft.Maui.Devices.DeviceInfo.Idiom == Microsoft.Maui.Devices.DeviceIdiom.Phone)
        {
            ButtonLayoutGrid.ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star) };
            ButtonLayoutGrid.RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) };
            Grid.SetRow(ConfirmButton, 0);
            Grid.SetColumn(ConfirmButton, 0);
            Grid.SetRow(CancelButton, 1);
            Grid.SetColumn(CancelButton, 0);
        }
        else
        {
            ButtonLayoutGrid.ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) };
            ButtonLayoutGrid.RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Auto) };
            Grid.SetRow(CancelButton, 0);
            Grid.SetColumn(CancelButton, 0);
            Grid.SetRow(ConfirmButton, 0);
            Grid.SetColumn(ConfirmButton, 1);
        }
    }

    public override void OnNavigatedTo(IReadOnlyDictionary<string, object?> parameters)
    {
        base.OnNavigatedTo(parameters);
        if (parameters.TryGetValue("Title", out var title))
        {
            TitleLabel.Text = title?.ToString() ?? "Confirm Action";
        }

        if (parameters.TryGetValue("message", out var message))
        {
            MessageLabel.Text = message?.ToString() ?? "Are you sure?";
        }
        if (parameters.TryGetValue("CancelButton", out var cancelText))
        {
            CancelButton.Text = cancelText?.ToString() ?? PixelsorterApp.Resources.Languages.AppStrings.common_Cancel;
        }
        if (parameters.TryGetValue("ConfirmButton", out var confirmText))
        {
            ConfirmButton.Text = confirmText?.ToString() ?? PixelsorterApp.Resources.Languages.AppStrings.common_OK;
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        SetResult(true);
        await IPopupService.Current.PopAsync(this);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        SetResult(false);
        await IPopupService.Current.PopAsync(this);
    }
}