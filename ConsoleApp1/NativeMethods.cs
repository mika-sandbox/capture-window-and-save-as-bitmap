// ------------------------------------------------------------------------------------------
//  Copyright (c) Natsuneko. All rights reserved.
//  Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace ConsoleApp1;

public static class NativeMethods
{
    public delegate bool DwmGetDxSharedSurfaceDelegate(IntPtr hWnd, out IntPtr phSurface, out long pAdapterLuid, out long pFmtWindow, out long pPresentFlags, out long pWin32KUpdateId);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static DwmGetDxSharedSurfaceDelegate DwmGetDxSharedSurface;

    static NativeMethods()
    {
        var ptr = GetProcAddress(GetModuleHandle("user32"), "DwmGetDxSharedSurface");
        DwmGetDxSharedSurface = Marshal.GetDelegateForFunctionPointer<DwmGetDxSharedSurfaceDelegate>(ptr);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);


    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}