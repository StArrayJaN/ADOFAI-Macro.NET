using Microsoft.Windows.ApplicationModel.Resources;

namespace ADOFAI_Macro.Source.Utils;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

public class StartMacro
{
    private static readonly ResourceLoader _resourceLoader = new();
    public readonly List<int> _keys;
    public List<double> _noteTimes;
    public volatile bool _isRunning;
    private Thread? _workerThread;
    public int _keyIndex;
    private double _offset;
    public readonly Lock _syncLock = new Lock();
    public readonly List<double> intervals;

    public readonly int _startIndex;
    public readonly double[] angleData;
    public double Offset
    {
        get
        {
            lock (_syncLock) return _offset;
        }
        set
        {
            lock (_syncLock) _offset = value;
        }
    }

    public StartMacro(List<int> keys, List<double> noteTimes,bool cmd = false,int startIndex = 0)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _noteTimes = noteTimes ?? throw new ArgumentNullException(nameof(noteTimes));
        intervals = noteTimes.Skip(1)
            .Select((time, index) => AppUtils.Max(time - _noteTimes[index], 500))
            .ToList();
        intervals.Add(10);
        _startIndex = startIndex;
        angleData = MainWindow.mainWindow.currentLevel.GetAngleData().ToArray();
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _workerThread = new Thread(WorkerLoop)
        {
            Name = "MacroThread",
            Priority = ThreadPriority.Highest // 提升时间精度
        };
        _workerThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        if (_workerThread is { IsAlive: true })
        {
            if (!_workerThread.Join(TimeSpan.FromSeconds(1))) // 等待1秒
            {
                _workerThread.Interrupt(); // 强制终止
            }
            _workerThread = null!;
        }
    }

    private void WorkerLoop()
    {
        try
        {
            _keyIndex = 0;
            int events = _startIndex + 1;
            double startTime = GetCurrentTime();
            while (_isRunning && events < _noteTimes.Count)
            {
                double currentTime = GetCurrentTime();
                double timeMilliseconds = currentTime - startTime + _noteTimes[_startIndex];

                // 批量处理符合条件的事件
                while (events < _noteTimes.Count &&
                       (_noteTimes[events] + Offset) <= timeMilliseconds)
                {
                    if(!angleData[events].Equals(999)) TriggerKeyPress(ref _keyIndex, events);
                    events++;
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    public double GetCurrentTime()
    {
        return AppUtils.currentTime();
    }

    private void TriggerKeyPress(ref int keyIndex, int currentIndex)
    {
        lock (_syncLock) // 保证线程安全的按键索引访问
        {
            if (keyIndex >= _keys.Count) keyIndex = 0;
            int keyCode = _keys[keyIndex];
            new Thread(() => WindowsNativeAPI.PressKey(keyCode, intervals[currentIndex] - 2)).Start();
            keyIndex++;
        }
    }
}