namespace GameLibrary.Behaviors;

/// <summary>
/// Implemented by list row view models that may want to opt out of the
/// right-click context menu and double-click "add" interactions (e.g. the
/// structural Else/End marker rows in the page actions list).
/// </summary>
public interface IContextMenuSuppressible
{
    /// <summary>True when this row should ignore context-menu and double-click actions.</summary>
    bool SuppressContextMenu { get; }
}
