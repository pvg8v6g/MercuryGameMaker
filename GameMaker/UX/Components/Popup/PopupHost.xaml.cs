using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace GameMaker.UX.Components.Popup;

/// <summary>
/// A full-window overlay that hosts a stack of <see cref="PopupShell"/> based popups. Each popup is
/// placed on its own scrim layer; showing a new popup dims and locks every popup beneath it (the scrim
/// swallows input and the layer below is disabled), mirroring the modal, stackable popups of the Java
/// engine. When the top popup closes the layer beneath is re-enabled and interaction resumes there.
/// </summary>
public sealed partial class PopupHost
{
    public static PopupHost? Current { get; private set; }

    private static readonly SolidColorBrush ScrimBrush = new(Color.FromArgb(160, 0, 0, 0));

    public PopupHost()
    {
        InitializeComponent();
        Current = this;
    }

    /// <summary>
    /// Shows <paramref name="popup"/> on top of the stack and awaits the result of its shell. The popup
    /// must contain a <see cref="PopupShell"/> (exposed via <see cref="IPopupView.Shell"/>).
    /// </summary>
    public async Task<ContentDialogResult> ShowAsync<T>(T popup) where T : FrameworkElement, IPopupView
    {
        // Lock the current top layer so its controls cannot be interacted with while the new popup is up.
        if (LayerRoot.Children.Count > 0)
        {
            LayerRoot.Children[^1].IsHitTestVisible = false;
        }

        var layer = new Grid
        {
            Background = ScrimBrush
        };

        popup.HorizontalAlignment = HorizontalAlignment.Center;
        popup.VerticalAlignment = VerticalAlignment.Center;
        layer.Children.Add(popup);

        LayerRoot.Children.Add(layer);
        LayerRoot.Visibility = Visibility.Visible;

        var result = await popup.Shell.Result;

        LayerRoot.Children.Remove(layer);

        if (LayerRoot.Children.Count > 0)
        {
            LayerRoot.Children[^1].IsHitTestVisible = true;
        }
        else
        {
            LayerRoot.Visibility = Visibility.Collapsed;
        }

        return result;
    }
}
