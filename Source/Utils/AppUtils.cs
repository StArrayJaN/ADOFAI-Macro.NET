using System.Diagnostics;
using System.Text;

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
    public static void LogError(params object[] messages) => Log.instance.Error(messages);
    public static void LogInfo(params object[] messages) => Log.instance.Info(messages);
    public static void LogWarning(params object[] messages) => Log.instance.Warning(messages);
    public static void LogDebugInfo(params object[] messages)
    {
        #if DEBUG
        Log.instance.Info(messages);
        #endif
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
    
    public class Log : IDisposable
    {
        public static readonly Log instance = new();

        private string filePath = $"{Directory.GetCurrentDirectory()}\\log-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
        private StreamWriter fileWriter;

        private Log()
        {
            if (!File.Exists(filePath))
            {
                fileWriter = new(File.Create(filePath));
                fileWriter.Flush();
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Info("App Exited");
                Dispose();
            };
        }

        public static void AutoLogException()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                instance.Error(e.ExceptionObject);
            };
        }

        public enum LogKind
        {
            Info,
            Warning,
            Error
        }

        private string GetTemplate(LogKind kind, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd-hh:mm:ss}/{Thread.CurrentThread.Name ?? $"Thread-{Thread.CurrentThread.ManagedThreadId}"}/{kind}]: {message}";
        }

        public void Info(params object[] messages)
        {
            fileWriter.WriteLine(GetTemplate(LogKind.Info, string.Join("\n", messages)));
            fileWriter.Flush();
        }

        public void Warning(params object[] messages)
        {
            fileWriter.WriteLine(GetTemplate(LogKind.Warning, string.Join("\n", messages)));
            fileWriter.Flush();
        }

        public void Error(params object[] messages)
        {
            fileWriter.WriteLine(GetTemplate(LogKind.Error, string.Join("\n", messages)));
            fileWriter.Flush();
        }

        public void Dispose()
        {
            fileWriter.Flush();
            fileWriter.Close();
        }

        ~Log()
        {
            fileWriter.Close();
        }
    }
}