using System.Runtime.InteropServices;
using Windows.System;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ADOFAI_Macro.Source.Utils;

public class WindowsNativeAPI
{
    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab = false);
    [DllImport("user32.dll")]
    public static extern uint SendInput(int nInputs, INPUT[] pInputs, int cbSize);
    
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
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
    
    [DllImport("user32.dll")]
    public static extern void AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    
    [DllImport("user32.dll")]
    static extern bool PostMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll")]
    static extern void NtUserSendInput([In] ref INPUT pInputs, [In] uint nInputs, [In] uint cbSize);

    // 窗口消息常量
    private const uint WM_SYSCOMMAND = 0x0112;
    private const uint SC_RESTORE = 0xF120;

    private static readonly ResourceLoader _resourceLoader = new();

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
    const int pane = 0;
    public static void PressKey(int key, double lengthMs = 1)
    {
        switch (pane)
        {
            case 0:
                keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, 0);
                if (lengthMs > 1) AppUtils.Sleep(lengthMs);
                keybd_event((byte)key, 0, KEYEVENTF_KEYUP, 0);
                break;
            case 1:
                INPUT[] inputs = new INPUT[2];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].U.ki.wVk = (ushort)key;
                inputs[0].U.ki.dwFlags = KEYEVENTF_KEYDOWN;
                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].U.ki.wVk = (ushort)key;
                inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;
                SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                break;
            case 2:
                SendKeyToWindow(key, lengthMs);
                break;
        }
    }
    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;
    const uint WM_CHAR = 0x0102;

// 向特定窗口发送按键
    public static bool SendKeyToWindow(int key, double lengthMs = 1)
    {
        nint hWnd = MainWindow.mainWindow._adofai._process.MainWindowHandle;
        // 创建LPARAM参数
        var keyDownLParam = CreateLParam(1, key, false, false, false);
        var keyUpLParam = CreateLParam(1, key, false, true, true);
        // 发送按键按下消息
        bool keyDownSent = PostMessageW(hWnd, WM_KEYDOWN, key, keyDownLParam);
        // 发送按键释放消息
        bool keyUpSent = PostMessageW(hWnd, WM_KEYUP, key, keyUpLParam);
        return keyDownSent && keyUpSent;
    }

    private static IntPtr CreateLParam(int repeatCount, int scanCode, bool extendedKey, 
        bool contextCode, bool previousState)
    {
        uint lParam = (uint)repeatCount;
        lParam |= (uint)(scanCode & 0xFF) << 16;        // 扫描码（低16位）
        lParam |= (extendedKey ? 1u : 0u) << 24;        // 扩展键标志
        lParam |= (contextCode ? 1u : 0u) << 29;        // 上下文代码
        lParam |= (previousState ? 1u : 0u) << 30;      // 先前状态
        lParam |= (previousState ? 1u : 0u) << 31;      // 转换状态

        return (IntPtr)lParam;
    }
    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;
    public const int HWND_TOPMOST = -1;
    public const int HWND_NOTOPMOST = -2;

    // SendInput constants
    public const int INPUT_KEYBOARD = 1;
    public const int KEYEVENTF_KEYDOWN = 0x0000;
    public const int KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

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