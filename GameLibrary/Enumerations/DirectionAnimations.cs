namespace GameLibrary.Enumerations;

// how an arte that turns with its user shows its animations for each way they face
public enum DirectionAnimations
{
    SeparateAnimations, // its animation facing down, the animation with Id + 1 facing left, + 2 right, + 3 up
    MoveAndRotate // one animation, moved and turned for each way they face
}
