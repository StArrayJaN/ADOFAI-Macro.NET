using System.Runtime.InteropServices;
using Windows.System;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ADOFAI_Macro.Source.Utils;

public class WindowsNative
{
    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab = false);
    [DllImport("user32.dll")]
    public static extern void keybd_event(int key, byte scan, uint flags, nint extraInfo);
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint wFlags);

    [DllImport("kernel32.dll")]
    public static extern int GetLastError();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // 窗口消息常量
    private const uint WM_SYSCOMMAND = 0x0112;
    private const uint SC_RESTORE = 0xF120;

    private readonly static ResourceLoader _resourceLoader = new();

    /// <summary>
    /// 设置窗体为TopMost
    /// </summary>
    /// <param name="hWnd"></param>
    public static void SetTopMost(IntPtr hWnd)
    {
        GetWindowRect(hWnd, out RECT rect);
        SetWindowPos(hWnd, HWND_TOPMOST, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, 0);
    }
    
    /// <summary>
    /// 直接通过窗口句柄设置大小
    /// </summary>
    public static bool SetWindowSize(IntPtr hWnd, int width, int height)
    {
        // 如果窗口最小化，先恢复
        SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)SC_RESTORE, IntPtr.Zero);

        // 获取当前窗口位置
        if (!GetWindowRect(hWnd, out RECT rect))
        {
            Console.WriteLine(_resourceLoader.GetString("WindowsNative_GetWindowRectangleFailureMessage"));
            return false;
        }

        // 计算新位置（保持窗口中心不变）
        int x = rect.left;
        int y = rect.top;

        // 设置新尺寸
        if (!MoveWindow(hWnd, x, y, width, height, true))
        {
            return false;
        }
        return true;
    }

    
    public static void PressKey(VirtualKey key, double lengthMs = 1)
    {
        keybd_event((int)key, 0, 0, 0);
        if (lengthMs > 1) AppUtils.Sleep(lengthMs);
        keybd_event((int)key, 0, 0x2, 0);
    }
    
    public static void PressKey(int key, double lengthMs = 1)
    {
        keybd_event(key, 0, 0, 0);
        if (lengthMs > 1) AppUtils.Sleep(lengthMs);
        keybd_event(key, 0, 0x2, 0);
    }
    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;
    public const int HWND_TOPMOST = -1;
    public const int HWND_NOTOPMOST = -2;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        [MarshalAs(UnmanagedType.I4)]
        public int x;
        [MarshalAs(UnmanagedType.I4)]
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        [MarshalAs(UnmanagedType.I4)]
        public int left;
        [MarshalAs(UnmanagedType.I4)]
        public int top;
        [MarshalAs(UnmanagedType.I4)]
        public int right;
        [MarshalAs(UnmanagedType.I4)]
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 length;
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 flags;
        [MarshalAs(UnmanagedType.U2)]
        public UInt16 showCmd;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT ptMinPosition;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT ptMaxPosition;
        [MarshalAs(UnmanagedType.Struct)]
        public RECT rcNormalPosition;
    }
}