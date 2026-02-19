using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Tasks;

public abstract class EngineTask : PropertyChangedUpdater
{
    #region Properties

    public int Work
    {
        get;
        set
        {
            SetField(ref field, value);
            Progress = MaxWork <= 0 ? 100 : (int) (100 * ((decimal) Work / (decimal) MaxWork));
        }
    }

    protected int MaxWork
    {
        get;
        set
        {
            SetField(ref field, value);
            Progress = MaxWork <= 0 ? 100 : (int) (100 * ((decimal) Work / (decimal) MaxWork));
        }
    }

    public int Progress
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Abstract Methods

    public abstract Task Call();

    #endregion
}
