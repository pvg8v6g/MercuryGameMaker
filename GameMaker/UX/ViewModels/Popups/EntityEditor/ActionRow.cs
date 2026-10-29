using GameLibrary.Behaviors;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Utilities;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.ViewModels.Popups.EntityEditor;

public enum ActionRowKind
{
    Normal,
    Else,
    End,
    Blank
}

/// <summary>
/// A single flattened row in the page actions list. Real actions are represented by
/// <see cref="ActionRowKind.Normal"/> rows; branches additionally produce structural
/// <see cref="ActionRowKind.Else"/>/<see cref="ActionRowKind.End"/> marker rows and
/// <see cref="ActionRowKind.Blank"/> "add here" rows which are presentation-only.
/// </summary>
public class ActionRow : PropertyChangedUpdater, IContextMenuSuppressible
{
    public PageAction? Action { get; init; }

    public ActionRowKind Kind { get; init; } = ActionRowKind.Normal;

    public int Indent { get; init; }

    /// <summary>The branch this row belongs to, or null for the top-level page action list.</summary>
    public BranchAction? ParentBranch { get; init; }

    /// <summary>True when this row lives inside its parent branch's else section.</summary>
    public bool InElse { get; init; }

    public Thickness IndentMargin => new(Indent * 24, 0, 0, 0);

    /// <summary>
    /// Category accent brush for the row's action, matching the colour-coded "Add" menu
    /// groups. Else/End branch markers use the Flow colour (they are branch-flow structure);
    /// blank rows fall back to the default text brush.
    /// </summary>
    public Brush Foreground =>
        (Kind == ActionRowKind.Normal && Action is not null
            ? EntityEditorViewModel.ColorForAction(Action)?.ToDarkenedBrush()
            : IsMarker
                ? ActionColor.Flow.ToDarkenedBrush()
                : null)
        ?? (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

    public bool IsMarker => Kind is ActionRowKind.Else or ActionRowKind.End;

    /// <summary>Else/End marker rows ignore context-menu and double-click interactions.</summary>
    public bool SuppressContextMenu => IsMarker;

    public string DisplayText => Kind switch
    {
        ActionRowKind.Else => "Else",
        ActionRowKind.End => "End",
        ActionRowKind.Blank => "◇",
        _ => Action?.ToString() ?? string.Empty
    };
}
