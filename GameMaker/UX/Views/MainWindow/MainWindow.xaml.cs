using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace GameMaker.UX.Views.MainWindow;

[SuppressMessage("Interoperability",
    "SYSLIB1054:Use \'LibraryImportAttribute\' instead of \'DllImportAttribute\' to generate P/Invoke marshalling code at compile time")]
public partial class MainWindow
{
    #region Properties

    public static MainWindow Window { get; private set; } = null!;

    #endregion

    #region Fields

    private const uint WmSeticon = 0x0080;
    private const IntPtr IconSmall = 0;
    private const IntPtr IconBig = 1;
    private const uint ImageIcon = 1;
    private const uint LrLoadfromfile = 0x0010;

    #endregion

    public MainWindow()
    {
        Window = this;
        InitializeComponent();

        var hWnd = WindowNative.GetWindowHandle(this);

        // Set Small Icon (Title Bar - 16x16 or 24x24)
        var smallIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Graphics/ico/24x24.ico");
        var hSmallIcon = LoadImage(IntPtr.Zero, smallIconPath, ImageIcon, 16, 16, LrLoadfromfile);
        SendMessage(hWnd, WmSeticon, IconSmall, hSmallIcon);

        // Set Big Icon (Taskbar - 32x32 or 48x48)
        var bigIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Graphics/ico/48x48.ico");
        var hBigIcon = LoadImage(IntPtr.Zero, bigIconPath, ImageIcon, 32, 32, LrLoadfromfile);
        SendMessage(hWnd, WmSeticon, IconBig, hBigIcon);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);
}
