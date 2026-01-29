using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameLibrary.Models;

public class PropertyChangedUpdater : INotifyPropertyChanged
{

    #region Functions

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion

}
