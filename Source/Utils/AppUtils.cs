using System.Diagnostics;

namespace ADOFAI_Macro.Source.Utils;

public class AppUtils
{
    private static readonly double tickToNanoseconds = 1_000_000_000.0 / Stopwatch.Frequency;
    
    /// <summary>
    /// 获取当前高精度纳秒时间戳（相对于不确定的起点）
    /// </summary>
    public static double GetNanoseconds()
    {
        return (Stopwatch.GetTimestamp() * tickToNanoseconds);
    }

    /// <summary>
    /// 获取从某个起点开始的纳秒时间
    /// </summary>
    public static double GetElapsedNanoseconds(long startingTimestamp)
    {
        return ((Stopwatch.GetTimestamp() - startingTimestamp) * tickToNanoseconds);
    }

    // TODO: 可能需要处理null
    public static string? GetResourcePath()
    {
        DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        string[] dirNames = dir.GetDirectories().Select(x => x.ToString()).ToArray();
        for (int i = 0; i < dirNames.Length; i++)
        {
            if (dirNames[i].Contains("resource"))
            {
                return dirNames[i];
            }
        }
        // TODO: 可能需要处理null
        if (dir.Parent != null)
        {
            dirNames = dir.Parent.GetDirectories().Select(x => x.ToString()).ToArray();
        }
        for (int i = 0; i < dirNames.Length; i++)
        {
            if (dirNames[i].Contains("resource"))
            {
                return dirNames[i];
            }
        }
        return null;
    }
    public static void Sleep(double ms) {
        double currentTime = AppUtils.currentTime();
        while (true) {
            if (currentTime + ms < AppUtils.currentTime()) {
                break;
            }
        }
    }

    public static double currentTime()
    {
        return GetNanoseconds() / 1_000_000.0;
    }

    public static double Max(double num, double num2)
    {
        return num > num2 ? num2 : num;
    }
}