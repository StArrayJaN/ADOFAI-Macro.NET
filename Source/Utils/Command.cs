namespace ADOFAI_Macro.Source.Utils;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Command : IDisposable
{
    private IntPtr _consoleHandle;
    private bool _ownsConsole;

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    
    public Command()
    {
        // 检查是否已有控制台窗口
        _consoleHandle = GetConsoleWindow();
        
        if (_consoleHandle == IntPtr.Zero)
        {
            // 如果没有控制台窗口，则创建一个
            _ownsConsole = AllocConsole();
            _consoleHandle = GetConsoleWindow();
            if (!_ownsConsole)
            {
                throw new Exception("无法创建控制台窗口");
            }
        }
        WindowsNative.SetTopMost(_consoleHandle);
        WindowsNative.SetWindowSize(_consoleHandle, 500, 200);
        AppUtils.LogDebugInfo("控制台:创建");
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
        AppUtils.LogDebugInfo($"控制台:{message}");
    }

    public void WriteLine(object value)
    {
        Console.WriteLine(value);
        AppUtils.LogDebugInfo($"控制台:{value}");
    }

    public void WriteLine(string format, params object[] args)
    {
        Console.WriteLine(format, args);
        AppUtils.LogDebugInfo($"控制台:{string.Format(format,args)}");
    }

    // TODO: 可能需要处理null
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void Clear()
    {
        try
        {
            Console.Clear();
            AppUtils.LogDebugInfo("控制台:清空");
        } catch(IOException){}
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 如果是我们创建的控制台，则释放它
            if (_ownsConsole && _consoleHandle != IntPtr.Zero)
            {
                FreeConsole();
                AppUtils.LogDebugInfo("控制台:销毁");
                _consoleHandle = IntPtr.Zero;
            }
        }
    }

    ~Command()
    {
        Dispose(false);
    }
}