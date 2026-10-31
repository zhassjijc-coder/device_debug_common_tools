using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace LinkLab.App;

internal static class StartupWindowPlacement
{
    // Work area and output are native pixels, including negative monitor origins.
    internal static Rect Bounds(Rect work, double scale)
    {
        double width = Math.Floor(Math.Min(1360 * scale, work.Width * .92));
        double height = Math.Floor(Math.Min(920 * scale, work.Height * .90));
        return new Rect(Math.Floor(work.Left + (work.Width - width) / 2),
            Math.Floor(work.Top + (work.Height - height) / 2), width, height);
    }

    internal static void Apply(Window window)
    {
        var area = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
        var handle = new WindowInteropHelper(window).Handle;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        // Move the still-hidden HWND onto the target monitor before reading DPI.
        // Relax the old fixed minimum so it cannot force a small screen to overflow.
        window.MinWidth = 1; window.MinHeight = 1;
        Position(handle, new Rect(area.Left, area.Top, Math.Min(1360, area.Width), Math.Min(920, area.Height)));
        double scale = Math.Max(96u, GetDpiForWindow(handle)) / 96d;
        var bounds = Bounds(new Rect(area.Left, area.Top, area.Width, area.Height), scale);
        window.MinWidth = Math.Min(1080, bounds.Width / scale);
        window.MinHeight = Math.Min(720, bounds.Height / scale);
        window.Width = bounds.Width / scale; window.Height = bounds.Height / scale;
        Position(handle, bounds);
    }

    private static void Position(IntPtr handle, Rect bounds)
    {
        if (!SetWindowPos(handle, IntPtr.Zero, (int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, 0x0014))
            throw new Win32Exception(); // SWP_NOZORDER | SWP_NOACTIVATE; never show a background launch.
    }
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
