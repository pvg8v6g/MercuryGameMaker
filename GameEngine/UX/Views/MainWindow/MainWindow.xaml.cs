namespace GameEngine.UX.Views.MainWindow;

public partial class MainWindow
{
    #region Properties

    public static MainWindow Window { get; private set; } = null!;

    #endregion

    public MainWindow()
    {
        Window = this;
        InitializeComponent();
    }
}
