namespace ADOFAI_Macro.Source.Utils;

public class FileWatcher
{
    private FileSystemWatcher? watcher;
    private Action? onFileChanged;
    private string filePath;

    public FileWatcher(string filePath, Action onFileChanged)
    {
        this.filePath = filePath;
        string dirPath = new FileInfo(filePath).Directory.ToString();
        this.onFileChanged = onFileChanged; 
        watcher = new FileSystemWatcher(dirPath);
        
        // 设置要监控的文件（可以使用通配符）
        watcher.Filter = "*.adofai";
        
        // 设置要监控的变化类型
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        // 启用事件
        watcher.EnableRaisingEvents = true;
       // 注册事件处理程序
        watcher.Changed += OnFileChanged;
    }

    public void Start()
    {
        watcher.EnableRaisingEvents = true;
        // 注册事件处理程序
        watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath == filePath) onFileChanged();
    }

    public void Stop()
    {
        watcher.Dispose();
    }
}