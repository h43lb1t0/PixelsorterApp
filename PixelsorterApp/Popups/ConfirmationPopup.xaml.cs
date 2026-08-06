using UXDivers.Popups;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

namespace PixelsorterApp.Popups;

public partial class ConfirmationPopup : PopupResultPage<bool>
{
    public ConfirmationPopup()
    {
        InitializeComponent();
    }

    public override void OnNavigatedTo(IReadOnlyDictionary<string, object?> parameters)
    {
        base.OnNavigatedTo(parameters);

        if (parameters.TryGetValue("message", out var message))
        {
            MessageLabel.Text = message?.ToString() ?? "Are you sure?";
        }
        if (parameters.TryGetValue("CancelButton", out var cancelText))
        {
            CancelButton.Text = cancelText?.ToString() ?? "Cancel";
        }
        if (parameters.TryGetValue("ConfirmButton", out var confirmText))
        {
            ConfirmButton.Text = confirmText?.ToString() ?? "Ok";
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