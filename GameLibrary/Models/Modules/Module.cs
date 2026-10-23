using System.Text.Json.Serialization;

namespace GameLibrary.Models.Modules;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(CharacterImageModule), "characterImage")]
[JsonDerivedType(typeof(EntityHitboxModule), "entityHitbox")]
[JsonDerivedType(typeof(InputModule), "input")]
[JsonDerivedType(typeof(MovementModule), "movement")]
[JsonDerivedType(typeof(PositionModule), "position")]
public abstract partial class Module : BaseModel;
