using System.Runtime.InteropServices;

namespace HylianGrimoire.Interop;

internal static partial class MouseCursor
{
    private const int ArrowCursorResourceId = 32512;

    public static void ResetToArrow()
    {
        IntPtr cursor = LoadCursor(IntPtr.Zero, ArrowCursorResourceId);
        if (cursor != IntPtr.Zero)
        {
            _ = SetCursor(cursor);
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static partial IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetCursor(IntPtr hCursor);
}
