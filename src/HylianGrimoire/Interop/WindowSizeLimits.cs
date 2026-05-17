using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace HylianGrimoire.Interop;

internal static partial class WindowSizeLimits
{
    private const uint WmGetMinMaxInfo = 0x0024;
    private const uint WmNcLeftButtonDoubleClick = 0x00A3;
    private const uint WmNcDestroy = 0x0082;
    private const uint WmSysCommand = 0x0112;
    private const int HitTestCaption = 2;
    private const nuint ScMaximize = 0xF030;
    private static readonly SubclassProc Callback = WndProc;
    private static readonly Dictionary<IntPtr, SizeLimits> MinimumSizes = [];

    public static void SetMinimumSize(Window window, int width, int height)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        MinimumSizes[hwnd] = new SizeLimits(width, height, null, null);

        _ = SetWindowSubclass(hwnd, Callback, UIntPtr.Zero, UIntPtr.Zero);
        window.Closed += (_, _) => Remove(hwnd);
    }

    public static void SetFixedWidth(Window window, int width, int minHeight)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        MinimumSizes[hwnd] = new SizeLimits(width, minHeight, width, null, SuppressMaximize: true);

        _ = SetWindowSubclass(hwnd, Callback, UIntPtr.Zero, UIntPtr.Zero);
        window.Closed += (_, _) => Remove(hwnd);
    }

    private static IntPtr WndProc(
        IntPtr hwnd,
        uint message,
        UIntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr refData)
    {
        if (MinimumSizes.TryGetValue(hwnd, out SizeLimits size)
            && size.SuppressMaximize
            && IsMaximizeMessage(message, wParam))
        {
            return IntPtr.Zero;
        }

        if (message == WmGetMinMaxInfo && MinimumSizes.TryGetValue(hwnd, out size))
        {
            var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            info.MinTrackSize.X = size.MinWidth;
            info.MinTrackSize.Y = size.MinHeight;
            if (size.MaxWidth is int maxWidth)
            {
                info.MaxTrackSize.X = maxWidth;
            }

            if (size.MaxHeight is int maxHeight)
            {
                info.MaxTrackSize.Y = maxHeight;
            }

            Marshal.StructureToPtr(info, lParam, fDeleteOld: false);
        }
        else if (message == WmNcDestroy)
        {
            Remove(hwnd);
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private static void Remove(IntPtr hwnd)
    {
        MinimumSizes.Remove(hwnd);
        _ = RemoveWindowSubclass(hwnd, Callback, UIntPtr.Zero);
    }

    private static bool IsMaximizeMessage(uint message, UIntPtr wParam)
    {
        if (message == WmNcLeftButtonDoubleClick && wParam == (UIntPtr)HitTestCaption)
        {
            return true;
        }

        return message == WmSysCommand && ((nuint)wParam & 0xFFF0) == ScMaximize;
    }

    private readonly record struct SizeLimits(
        int MinWidth,
        int MinHeight,
        int? MaxWidth,
        int? MaxHeight,
        bool SuppressMaximize = false);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point Reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
    }

    private delegate IntPtr SubclassProc(
        IntPtr hwnd,
        uint message,
        UIntPtr wParam,
        IntPtr lParam,
        UIntPtr subclassId,
        UIntPtr refData);

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(
        IntPtr hwnd,
        SubclassProc subclassProc,
        UIntPtr subclassId,
        UIntPtr refData);

    [LibraryImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveWindowSubclass(
        IntPtr hwnd,
        SubclassProc subclassProc,
        UIntPtr subclassId);

    [LibraryImport("comctl32.dll")]
    private static partial IntPtr DefSubclassProc(
        IntPtr hwnd,
        uint message,
        UIntPtr wParam,
        IntPtr lParam);
}
