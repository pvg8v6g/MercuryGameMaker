using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameLibrary.Models;

/// <summary>
/// An icon: which icon sheet it's on (a picture in Graphics\Icons named for its theme, such as "Armor.png") and its cell on that sheet.
/// A sheet of null means the game's first sheet. Cell 0 of every sheet is left empty, so an index of 0 is no icon.
/// </summary>
[JsonConverter(typeof(GameIconJsonConverter))]
public sealed record GameIcon(string? Sheet, int Index)
{
    public static GameIcon None { get; } = new(null, 0);

    public bool IsNone => Index <= 0;

    public override string ToString() => IsNone ? "None" : $"{Sheet ?? "first sheet"} #{Index}";
}

/// <summary>
/// Saved as {"Sheet": "Armor.png", "Index": 12}, or as a plain number when it has no sheet (no icon, or data that
/// only has one sheet). Plain numbers from older files are read as a cell on the first sheet.
/// </summary>
public sealed class GameIconJsonConverter : JsonConverter<GameIcon>
{
    public override GameIcon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return GameIcon.None;
            case JsonTokenType.Number:
                return new GameIcon(null, reader.GetInt32());
            case JsonTokenType.StartObject:
                string? sheet = null;
                var index = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;

                    var name = reader.GetString();
                    reader.Read();
                    if (string.Equals(name, nameof(GameIcon.Sheet), StringComparison.OrdinalIgnoreCase)) sheet = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    else if (string.Equals(name, nameof(GameIcon.Index), StringComparison.OrdinalIgnoreCase) && reader.TokenType == JsonTokenType.Number) index = reader.GetInt32();
                    else reader.Skip();
                }

                return new GameIcon(string.IsNullOrEmpty(sheet) ? null : sheet, index);
            default:
                throw new JsonException($"An icon can't be read from {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, GameIcon value, JsonSerializerOptions options)
    {
        if (value.Sheet is null)
        {
            writer.WriteNumberValue(value.Index);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString(nameof(GameIcon.Sheet), value.Sheet);
        writer.WriteNumber(nameof(GameIcon.Index), value.Index);
        writer.WriteEndObject();
    }
}
