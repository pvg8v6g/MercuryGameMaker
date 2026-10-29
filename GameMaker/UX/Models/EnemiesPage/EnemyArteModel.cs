using GameLibrary.Models.Artes;
using GameLibrary.Models.Fighter;

namespace GameMaker.UX.Models.EnemiesPage;

public class EnemyArteModel(FighterArte fighterArte, Arte? arte)
{
    #region Properties

    public FighterArte FighterArte => fighterArte;

    public string Name => arte?.Name ?? "(Missing Arte)";

    public string ConditionText => fighterArte.Conditions.Count == 0
        ? "Always"
        : string.Join(", ", fighterArte.Conditions.Select(x => x.ToString()));

    #endregion
}
