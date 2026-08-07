using System.Windows.Input;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

using System.Collections.Generic;

namespace PixelsorterApp.Popups;

public partial class NavigationPopup : PopupResultPage<String>
{
    private ICommand SetResultCommand;
    public NavigationPopup()
    {
        InitializeComponent();
        SetResultCommand = new Command(async result => 
        { 
            SetResult(result?.ToString()); 
            await IPopupService.Current.PopAsync(this);
        });
    }


    public override void OnNavigatedTo(IReadOnlyDictionary<string, object?> parameters)
    {
        base.OnNavigatedTo(parameters);
        
        navButtonsGrid.Children.Clear();

        if (parameters.TryGetValue("Message", out var message)) {
            MessageLabel.Text = message?.ToString();
        }

        if (parameters.TryGetValue("Options", out var options) && options is IEnumerable<string> buttonOptions) {
            foreach (var option in buttonOptions) {
                navButtonsGrid.Add(
                    new Button()
                    {
                        Text = option,
                        Command = SetResultCommand,
                        CommandParameter = option,
                        Margin = new Thickness(6)
                    }
                );
            }
        }
    }
}