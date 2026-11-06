using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameData;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// An actor that can be in the starting party. The leader is always in it, so its box can't be unchecked.
/// </summary>
public partial class PartyMemberModel(Fighter actor, GameStart start) : PropertyChangedUpdater
{
    #region Properties

    public string Name => actor.Name;

    public bool IsLeader => start.Party.FirstOrDefault() == actor.Guid;

    public bool CanToggle => !IsLeader;

    public bool IsInParty
    {
        get => start.Party.Contains(actor.Guid);
        set
        {
            if (value == IsInParty || IsLeader) return;
            if (value) start.Party.Add(actor.Guid);
            else start.Party.Remove(actor.Guid);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Methods

    public void Refresh()
    {
        OnPropertyChanged(nameof(IsLeader));
        OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(IsInParty));
    }

    #endregion
}
