using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace LinkLab.App;

internal static class WindowPlacementSmoke
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr handle);
    private static void Check(bool ok, string message) { if (!ok) throw new IOException("Window placement: " + message); }

    internal static async Task Run(string output)
    {
        foreach (var work in new[] { new Rect(0, 0, 1366, 728), new Rect(-2560, 40, 2560, 1400), new Rect(1920, -900, 1080, 1880), new Rect(0, 0, 3840, 2120) })
        foreach (double scale in new[] { 1, 1.25, 1.5, 2, 3 })
        {
            var bounds = StartupWindowPlacement.Bounds(work, scale);
            Check(work.Contains(bounds), "scaled window exceeds work area");
            Check(Math.Abs(bounds.Left + bounds.Width / 2 - work.Left - work.Width / 2) <= 1 &&
                Math.Abs(bounds.Top + bounds.Height / 2 - work.Top - work.Height / 2) <= 1, "window is not centered");
        }
        var vm = new ShellViewModel(false, SystemSettingsSmoke.CreateRegistration());
        var window = new MainWindow(vm, false) { ShowActivated = false, ShowInTaskbar = false };
        try
        {
            // Create the real HWND while hidden, including startup placement and DPI transitions.
            var handle = new WindowInteropHelper(window).EnsureHandle();
            Check(!window.IsVisible, "startup placement exposed a background window");
            var area = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
            double scale = Math.Max(96u, GetDpiForWindow(handle)) / 96d;
            var expected = StartupWindowPlacement.Bounds(new Rect(area.Left, area.Top, area.Width, area.Height), scale);
            Check(GetWindowRect(handle, out var actual), "cannot read native bounds");
            Check(Math.Abs(actual.Left - expected.Left) <= 1 && Math.Abs(actual.Top - expected.Top) <= 1 &&
                Math.Abs(actual.Right - actual.Left - expected.Width) <= 1 && Math.Abs(actual.Bottom - actual.Top - expected.Height) <= 1, "native bounds differ from centered work-area bounds");
            Check(window.MinWidth <= window.Width && window.MinHeight <= window.Height, "minimum forces overflow");
            window.Opacity = 0; window.Show();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Check(GetWindowRect(handle, out actual) && Math.Abs(actual.Left - expected.Left) <= 1 && Math.Abs(actual.Top - expected.Top) <= 1 &&
                Math.Abs(actual.Right - actual.Left - expected.Width) <= 1 && Math.Abs(actual.Bottom - actual.Top - expected.Height) <= 1, "first Show changed the fitted bounds");
        }
        finally { window.Close(); await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); }
        File.WriteAllText(Path.Combine(output, "window-placement-result.txt"), "PASS: 20 geometry cases (small/portrait/4K/negative origin; 100-300% DPI); native HWND fits and centers on the current monitor; background startup stays hidden.\n");
    }
}
