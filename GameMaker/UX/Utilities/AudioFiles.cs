using GameLibrary.Models.Media;
using MercuryLibrary.Extensions;

namespace GameMaker.UX.Utilities;

/// <summary>
/// The editor's audio pickers: a list of file names with "(None)" first, and turning a pick back into an AudioFile.
/// </summary>
public static class AudioFiles
{
    #region Constants

    public const string None = "(None)";

    #endregion

    #region Methods

    // the picked name kept on the existing file (so its volume and pitch stay), null for "(None)"
    public static AudioFile? FromName(AudioFile? current, string name, string path)
    {
        if (name == None || name.IsNullOrEmpty()) return null;
        current ??= new AudioFile { Path = path };
        current.Name = name;
        return current;
    }

    public static string NameOf(AudioFile? file) => file?.Name ?? None;

    // the file names in a folder, empty when it doesn't exist
    public static string[] InFolder(string path)
    {
        return Directory.Exists(path) ? [.. Directory.GetFiles(path).Select(Path.GetFileName).OfType<string>()] : [];
    }

    #endregion
}
