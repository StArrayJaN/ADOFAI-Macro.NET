using System.Runtime.InteropServices;

namespace ADOFAI_Macro.Source.Utils;
//Mono函数调用测试
public class Mono
{
    #region C语言函数
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    #endregion
    private IntPtr monoDllHandle;
    
    public Mono(IntPtr baseAddr)
    {
        monoDllHandle = baseAddr;
    }
    public IntPtr GetMonoMethod(string methodName)
    {
        return GetProcAddress(monoDllHandle, methodName);    
    }
}