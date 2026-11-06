using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using GameLibrary.Enumerations;
using System.Text.RegularExpressions;

namespace GameMaker.UX.Components.EngineTextBox;

public sealed partial class EngineTextBox
{
    #region Dependency Properties

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(EngineTextBox), new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string) GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(EngineTextBox), new PropertyMetadata(null));

    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(EngineTextBox), new PropertyMetadata(string.Empty));

    public string PlaceholderText
    {
        get => (string) GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
        nameof(MaxLength), typeof(int), typeof(EngineTextBox), new PropertyMetadata(0));

    public int MaxLength
    {
        get => (int) GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public static readonly DependencyProperty InputTypeProperty = DependencyProperty.Register(
        nameof(InputType), typeof(InputType), typeof(EngineTextBox), new PropertyMetadata(InputType.Default));

    public InputType InputType
    {
        get => (InputType) GetValue(InputTypeProperty);
        set => SetValue(InputTypeProperty, value);
    }

    public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register(
        nameof(AcceptsReturn), typeof(bool), typeof(EngineTextBox), new PropertyMetadata(false));

    public bool AcceptsReturn
    {
        get => (bool) GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
        nameof(TextWrapping), typeof(TextWrapping), typeof(EngineTextBox), new PropertyMetadata(TextWrapping.NoWrap));

    public TextWrapping TextWrapping
    {
        get => (TextWrapping) GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public static readonly DependencyProperty TooltipProperty = DependencyProperty.Register(
        nameof(Tooltip), typeof(object), typeof(EngineTextBox), new PropertyMetadata(null));

    public object? Tooltip
    {
        get => (object?) GetValue(TooltipProperty);
        set => SetValue(TooltipProperty, value);
    }

    // PropertyChanged sends every keystroke, LostFocus waits for enter or for the box to lose focus
    public static readonly DependencyProperty UpdateTriggerProperty = DependencyProperty.Register(
        nameof(UpdateTrigger), typeof(UpdateSourceTrigger), typeof(EngineTextBox), new PropertyMetadata(UpdateSourceTrigger.PropertyChanged));

    public UpdateSourceTrigger UpdateTrigger
    {
        get => (UpdateSourceTrigger) GetValue(UpdateTriggerProperty);
        set => SetValue(UpdateTriggerProperty, value);
    }

    #endregion

    #region Fields

    private string _oldText = string.Empty;

    #endregion

    #region Constructor

    public EngineTextBox()
    {
        InitializeComponent();
    }

    #endregion

    #region Listeners

    private void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        var newText = sender.Text;

        if (string.IsNullOrEmpty(newText))
        {
            _oldText = newText;
            return;
        }

        if (IsValid(newText))
        {
            _oldText = newText;
        }
        else
        {
            var selectionStart = sender.SelectionStart;
            var selectionLength = sender.SelectionLength;

            sender.Text = _oldText;
            sender.SelectionStart = Math.Max(0, selectionStart - 1);
            sender.SelectionLength = selectionLength;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (UpdateTrigger != UpdateSourceTrigger.LostFocus) Commit((TextBox) sender, false);
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        Commit((TextBox) sender, true);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !AcceptsReturn) Commit((TextBox) sender, true);
    }

    #endregion

    #region Private Methods

    // number boxes are bound to number properties, and the binding throws on text that isn't a number ("" or "-"
    // while retyping), so only whole numbers are sent. Finishing on one that isn't puts the last value back
    private void Commit(TextBox textBox, bool finished)
    {
        if (!IsNumber(textBox.Text))
        {
            if (finished) textBox.Text = Text;
            return;
        }

        if (Text != textBox.Text) Text = textBox.Text;
    }

    private bool IsNumber(string text) => InputType switch
    {
        InputType.Integer => int.TryParse(text, out _),
        InputType.Double => double.TryParse(text, out _),
        _ => true
    };

    private bool IsValid(string textToCheck)
    {
        if (MaxLength > 0 && textToCheck.Length > MaxLength)
        {
            return false;
        }

        return InputType switch
        {
            InputType.Default => true,
            InputType.Integer => Regex.IsMatch(textToCheck, "^-?(\\d+)?$"),
            InputType.Double => Regex.IsMatch(textToCheck, "^-?\\d*(.)?\\d*$"),
            _ => true
        };
    }

    #endregion
}
