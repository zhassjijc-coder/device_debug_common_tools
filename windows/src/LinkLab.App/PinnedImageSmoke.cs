using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LinkLab.App;

internal static class PinnedImageSmoke
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageW(IntPtr window, uint message, IntPtr wParam, ref NativeRect rect);
    private static void Check(bool value, string message) { if (!value) throw new IOException("Pin lifecycle smoke: " + message); }

    internal static async Task Run(string output)
    {
        var image = BitmapSource.Create(320, 160, 96, 96, PixelFormats.Bgra32, null, new byte[320 * 160 * 4], 320 * 4); image.Freeze();
        foreach (var theme in new[] { "Aero2", "Aero", "AeroLite", "Classic" })
        {
            var pins = new List<PinnedImageWindow>();
            try
            {
                // Same pixels may be pinned repeatedly, but the visual trees must be independent.
                for (int i = 0; i < 3; i++)
                {
                    var pin = new PinnedImageWindow(image, _ => { }, _ => { })
                    { WindowStartupLocation = WindowStartupLocation.Manual, Left = -16000, Top = 0, Topmost = false, ShowActivated = false };
                    pins.Add(pin);
                    pin.Resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri($"/PresentationFramework.{theme};component/themes/{theme.ToLowerInvariant()}.{(theme == "Classic" ? "" : "normalcolor.")}xaml", UriKind.Relative) });
                    pin.Show();
                    var handle = new WindowInteropHelper(pin).Handle;
                    // Exercise WPF's actual native DPI message path without changing display settings.
                    foreach (int dpi in new[] { 120, 144, 192, 96 })
                    {
                        Check(GetWindowRect(handle, out var rect), "window rectangle unavailable");
                        SendMessageW(handle, 0x02E0, new IntPtr(dpi | (dpi << 16)), ref rect);
                        await pin.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        pin.UpdateLayout();
                        var content = (FrameworkElement)pin.Content;
                        Check(App.Descendants<ContentPresenter>(pin).Count(p => ReferenceEquals(p.Content, content)) == 1, "content has multiple presenters");
                        Check(VisualTreeHelper.GetParent(content) is ContentPresenter, "content is attached to the wrong parent");
                        Check(ReferenceEquals(App.Descendants<Image>(pin).Single().Source, image), "pin changed original pixels");
                    }
                    Check(pins.Select(p => p.Content).Distinct().Count() == pins.Count, "windows share a content visual");
                }
                // Close while a deferred DPI resize is still queued.
                var closing = pins[^1]; var closingHandle = new WindowInteropHelper(closing).Handle;
                Check(GetWindowRect(closingHandle, out var closingRect), "closing rectangle unavailable");
                SendMessageW(closingHandle, 0x02E0, new IntPtr(144 | (144 << 16)), ref closingRect);
                closing.Close(); pins.Remove(closing);
                await closing.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            }
            finally { foreach (var pin in pins) pin.Close(); }
        }
        File.WriteAllText(Path.Combine(output, "pin-lifecycle-result.txt"), "PASS: 12 independent pins sharing one bitmap; Aero2/Aero/AeroLite/Classic resource dictionaries; native DPI notifications at 100/125/150/200%; close with queued resize. Executed on the current host, not a Windows 10 OS certification.\n");
    }
}
