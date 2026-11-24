using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using GameEngine.Models.Battle;
using GameEngine.Services.Input;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameEngine.Interface;

/// <summary>
/// The screen layer shown when a battle is won, after the party has picked up what the enemies left: what each party
/// member got (EXP and level-ups), then the money and items taken. It stops the game and plays the game's victory
/// sound effect. Enter or Space closes it, and the battle ends behind it.
/// The game loop starts and closes it (<see cref="PromptScreen.IsActive"/> changes at once), the rows are made on the ui thread.
/// </summary>
public partial class VictoryScreen(
    InterfaceImages images,
    IGameDataService gameDataService,
    IInputService inputService,
    IAudioService audioService) : PromptScreen(inputService)
{
    #region Fields

    // the Victory SFX playing: the screen goes away when it's over (without one it waits for a key)
    private volatile Task? _sound;

    #endregion

    #region Properties

    // what a key press would do, empty when the screen closes itself when the sound effect ends
    public string PromptText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public ObservableCollection<VictoryMember> Members { get; } = [];

    // the money first, then each kind of item
    public ObservableCollection<VictoryItem> Items { get; } = [];

    public string ExperienceText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public bool HasItems
    {
        get;
        private set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    public bool HasNoItems => !HasItems;

    #endregion

    #region Methods

    // called from the game loop when the battle is won and the pickup time is over
    public void Show(BattleRewards rewards)
    {
        Begin();

        _sound = gameDataService.GameSettings.VictorySfx is { Name: { Length: > 0 } } sfx ? PlaySound(sfx.Name, sfx.Volume, sfx.Pitch) : null;

        _ = Open(rewards);
    }

    #endregion

    #region Protected Methods

    // the sound effect is over
    protected override bool ClosesItself() => _sound is { IsCompleted: true };

    protected override void Close()
    {
        // closed early with a key: the sound doesn't carry on over the story map
        audioService.StopSfx();
        base.Close();
        Members.Clear();
        Items.Clear();
    }

    #endregion

    #region Private Methods

    // finishes when the sound is over. One that can't be played counts as over too, so the screen never waits on it
    private async Task PlaySound(string name, double volume, double pitch)
    {
        try
        {
            await audioService.PlaySfxToEnd(name, volume, pitch);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"The Victory SFX '{name}' could not be played: {exception.Message}");
        }
    }

    // the faces and icons are measured off the ui thread, then the rows are made on it. The screen opens even when a
    // picture can't be found, or the battle would wait on it forever
    private async Task Open(BattleRewards rewards)
    {
        var faces = new List<(string path, Rect cell)?>();
        var icons = new List<(string path, Rect cell)?>();
        try
        {
            foreach (var member in rewards.Members) faces.Add(await images.FaceCell(member.FaceName, member.FaceIndex));

            if (rewards.Money > 0) icons.Add(await images.IconCell(gameDataService.GameSettings.MoneyIcon));
            foreach (var item in rewards.Items) icons.Add(await images.IconCell(item.Icon));
        }
        finally
        {
            Dispatcher.TryEnqueue(() => Build(rewards, faces, icons));
        }
    }

    private void Build(BattleRewards rewards, List<(string path, Rect cell)?> faces, List<(string path, Rect cell)?> icons)
    {
        Members.Clear();
        Items.Clear();
        ExperienceText = rewards.Experience > 0 ? $"{rewards.Experience:N0} EXP" : string.Empty;

        for (var i = 0; i < rewards.Members.Count; i++)
        {
            var member = rewards.Members[i];
            var row = new HudFighter(member.Name, false);
            if (faces.ElementAtOrDefault(i) is { } face)
            {
                row.Face = new BitmapImage(new Uri(face.path));
                row.FaceRect = face.cell;
            }

            var leveled = member.LevelAfter > member.LevelBefore;
            var level = leveled ? $"Level {member.LevelBefore} → {member.LevelAfter}" : $"Level {member.LevelAfter}";
            var experience = member.Fallen ? "Down: no EXP" : $"+{member.Experience:N0} EXP";
            Members.Add(new VictoryMember(row, level, experience, leveled));
        }

        var next = 0;
        if (rewards.Money > 0) Items.Add(new VictoryItem($"{rewards.Money:N0} {gameDataService.GameSettings.MoneyName}", 1, icons.ElementAtOrDefault(next++)));
        foreach (var item in rewards.Items) Items.Add(new VictoryItem(item.Name, item.Count, icons.ElementAtOrDefault(next++)));

        HasItems = Items.Count > 0;
        PromptText = gameDataService.GameSettings.VictorySfx is { Name: { Length: > 0 } } ? string.Empty : "Press Enter to continue";
        Reveal();
    }

    #endregion
}
