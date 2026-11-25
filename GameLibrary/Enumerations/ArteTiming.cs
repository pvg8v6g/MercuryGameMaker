namespace GameLibrary.Enumerations;

// when in a fight the AI may use an arte
public enum ArteTiming
{
    AnyTime,
    Opening, // while the other side still has most of its Life
    Late, // once the other side is under half of its Life
    LowLifeTarget // when it would hit someone who's nearly down
}
