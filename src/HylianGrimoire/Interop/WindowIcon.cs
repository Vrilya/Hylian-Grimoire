using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace HylianGrimoire.Interop;

public static partial class WindowIcon
{
    private const int ApplicationIconResourceId = 32512;
    private const uint ImageIcon = 1;
    private const uint WmSetIcon = 0x0080;
    private static readonly IntPtr IconSmall = 0;
    private static readonly IntPtr IconBig = 1;
    private static readonly IntPtr ApplicationIconResource = ApplicationIconResourceId;

    public static void Apply(Window window)
    {
        IntPtr moduleHandle = GetModuleHandle(null);
        if (moduleHandle == IntPtr.Zero)
        {
            return;
        }

        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        SetWindowIcon(hwnd, moduleHandle, IconSmall, 16);
        SetWindowIcon(hwnd, moduleHandle, IconBig, 32);
    }

    private static void SetWindowIcon(IntPtr hwnd, IntPtr moduleHandle, IntPtr iconKind, int size)
    {
        IntPtr icon = LoadImage(moduleHandle, ApplicationIconResource, ImageIcon, size, size, 0);
        if (icon != IntPtr.Zero)
        {
            _ = SendMessage(hwnd, WmSetIcon, iconKind, icon);
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? moduleName);

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true)]
    private static partial IntPtr LoadImage(IntPtr instance, IntPtr name, uint type, int width, int height, uint load);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
