using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace HylianGrimoire.Interop;

internal static partial class WindowTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;

    private static readonly Windows.UI.ViewManagement.UISettings UiSettings = new();
    private static readonly List<WeakReference<Window>> RegisteredWindows = [];
    private static bool _listening;

    public static void Register(Window window)
    {
        RegisteredWindows.Add(new WeakReference<Window>(window));
        window.Closed += (_, _) => RemoveClosedWindows();

        ApplySystemTheme(window);

        if (_listening)
        {
            return;
        }

        UiSettings.ColorValuesChanged += (_, _) => ApplyToRegisteredWindows();
        _listening = true;
    }

    private static void ApplyToRegisteredWindows()
    {
        RemoveClosedWindows();
        foreach (var reference in RegisteredWindows.ToArray())
        {
            if (!reference.TryGetTarget(out var window))
            {
                continue;
            }

            DispatcherQueue dispatcherQueue = window.DispatcherQueue;
            _ = dispatcherQueue.TryEnqueue(() => ApplySystemTheme(window));
        }
    }

    private static void ApplySystemTheme(Window window)
    {
        ApplyContentTheme(window);
        ApplySystemTitleBarTheme(window);
    }

    private static void ApplyContentTheme(Window window)
    {
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = ShouldUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private static void ApplySystemTitleBarTheme(Window window)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        int useImmersiveDarkMode = ShouldUseDarkMode() ? 1 : 0;
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        int result = DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ref useImmersiveDarkMode, sizeof(int));
        if (result != 0)
        {
            _ = DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkModeBefore20H1, ref useImmersiveDarkMode, sizeof(int));
        }
    }

    private static void RemoveClosedWindows()
    {
        RegisteredWindows.RemoveAll(reference => !reference.TryGetTarget(out _));
    }

    private static bool ShouldUseDarkMode()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(AppsUseLightTheme) is int value && value == 0;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
