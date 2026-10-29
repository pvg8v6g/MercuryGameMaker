namespace GameMaker.UX.Components.Popup;

/// <summary>
/// Implemented by popup views hosted inside the <see cref="PopupHost"/>. Exposes the view's
/// <see cref="PopupShell"/> so the host can observe its result and unstack it when closed.
/// </summary>
public interface IPopupView
{
    PopupShell Shell { get; }
}
