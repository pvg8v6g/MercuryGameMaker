using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace GameMaker.UX.Components.Popup;

/// <summary>
/// Reusable chrome for a stacked popup hosted by <see cref="PopupHost"/>. Provides a title, an inner
/// content area and the Done/Cancel buttons, and exposes a <see cref="Result"/> task that completes
/// when the user closes the popup. This replaces <see cref="ContentDialog"/> for editors that need to
/// open on top of one another (e.g. entity editor -> movement route -> movement command).
/// </summary>
[ContentProperty(Name = nameof(PopupContent))]
public sealed partial class PopupShell
{
    #region Registered Properties

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(PopupShell), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string) GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty PopupContentProperty = DependencyProperty.Register(
        nameof(PopupContent), typeof(object), typeof(PopupShell), new PropertyMetadata(null));

    public object? PopupContent
    {
        get => GetValue(PopupContentProperty);
        set => SetValue(PopupContentProperty, value);
    }

    public static readonly DependencyProperty PrimaryButtonTextProperty = DependencyProperty.Register(
        nameof(PrimaryButtonText), typeof(string), typeof(PopupShell), new PropertyMetadata("OK"));

    public string PrimaryButtonText
    {
        get => (string) GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    public static readonly DependencyProperty SecondaryButtonTextProperty = DependencyProperty.Register(
        nameof(SecondaryButtonText), typeof(string), typeof(PopupShell),
        new PropertyMetadata(null, OnSecondaryButtonTextChanged));

    public string? SecondaryButtonText
    {
        get => (string?) GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    private static void OnSecondaryButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PopupShell shell)
        {
            shell.SecondaryButton.Visibility =
                string.IsNullOrEmpty(shell.SecondaryButtonText) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    #endregion

    #region Result

    private readonly TaskCompletionSource<ContentDialogResult> _result = new();

    /// <summary>Completes with the button the user pressed once the popup is closed.</summary>
    public Task<ContentDialogResult> Result => _result.Task;

    /// <summary>Raised when the popup is closed, so the host can unstack it.</summary>
    public event Action<PopupShell, ContentDialogResult>? Closed;

    #endregion

    public PopupShell()
    {
        InitializeComponent();
        SecondaryButton.Visibility =
            string.IsNullOrEmpty(SecondaryButtonText) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => Close(ContentDialogResult.Primary);

    private void OnSecondaryClick(object sender, RoutedEventArgs e) => Close(ContentDialogResult.Secondary);

    private void Close(ContentDialogResult result)
    {
        if (!_result.TrySetResult(result)) return;
        Closed?.Invoke(this, result);
    }
}
