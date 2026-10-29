using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Components.EngineListView;

/// <summary>
/// Describes a single typed entry in the <see cref="EngineListView"/> right-click "Add" submenu,
/// used when a list supports creating several different item types (e.g. page conditions).
/// </summary>
public class EngineListNewOption
{
    public string Text { get; init; } = string.Empty;

    public ICommand? Command { get; init; }

    public object? CommandParameter { get; init; }

    /// <summary>
    /// Optional accent colour applied to the menu entry's text, letting related option groups be
    /// visually segregated (mirrors the colour-coded action buttons in the Java editor).
    /// When unset, child options inherit their parent group's colour.
    /// </summary>
    public Brush? Foreground { get; init; }

    /// <summary>
    /// When set, this option becomes a submenu whose entries are the supplied child options,
    /// allowing the "Add" menu to be organised into categories (e.g. movement groups).
    /// </summary>
    public IReadOnlyList<EngineListNewOption>? Children { get; init; }
}
