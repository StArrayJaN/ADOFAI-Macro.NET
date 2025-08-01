using Json;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ADOFAI_Macro.Source.Utils;

public static class LevelUtils
{
    public static List<double> GetNoteTimes(ADOFAI.Level l)
    {
        List<double> angleDataList = l.GetAngleData();
        JsonArray levelEvents = l.actions;

        // 处理带有变速和旋转的中旋
        for (int i = 0; i < angleDataList.Count; i++)
        {
            if (Math.Abs(angleDataList[i] - 999) < 0.01)
            {
                if (l.HasEvent(i, "SetSpeed"))
                {
                    int index = l.GetEventIndex(i, "SetSpeed");
                    JsonObject? a = levelEvents[index].AsJsonObject;
                    if (a != null) {
                        a["floor"] = a["floor"].AsInteger + 1; 
                    }
                }
                else if (l.HasEvent(i, "Twirl"))
                {
                    int index = l.GetEventIndex(i, "Twirl");
                    JsonObject? a = levelEvents[index].AsJsonObject;
                    if (a != null) {
                        a["floor"] = a["floor"].AsInteger + 1;
                    }
                }
            }
        }

        JsonArray parsedChart = new JsonArray();
        int midrCount = 0;
        List<int> midrId = new List<int>();

        // 初步处理轨道数据
        for (int i = 0; i < angleDataList.Count; i++)
        {
            double angleData = angleDataList[i];
            if (Math.Abs(angleData - 999) < 0.01)
            {
                midrCount++;
                JsonObject? temp = parsedChart[i - midrCount].AsJsonObject;
                if (temp != null) {
                    temp["midr"] = "true";
                    parsedChart[i - midrCount] = temp;
                }
                midrId.Add(i - 1);
            }
            else
            {
                JsonObject temp = new JsonObject()
                    .Add("angle", Fmod(angleData, 360))
                    .Add("bpm", "unSet")
                    .Add("direction", 0)
                    .Add("extraHold", 0)
                    .Add("midr", false)
                    .Add("MultiPlanet", "-1");

                parsedChart.Add(temp);
            }
        }

        // 添加结束节点
        var endNode = new JsonObject()
            .Add("angle", Fmod(angleDataList[^1], 360))
            .Add("bpm", "unSet")
            .Add("direction", 0)
            .Add("extraHold", 0)
            .Add("midr", false)
            .Add("MultiPlanet", "-1");

        parsedChart.Add(endNode);

        double bpm = l.GetBPM();
        double pitch = l.GetPitch() / 100.0;
        
        // 处理事件数据
        foreach (var eventValue in levelEvents)
        {
            JsonObject? o = eventValue.AsJsonObject;
            if (o != null) {
                int tile = o["floor"].AsInteger;
                string? eventType = o["eventType"].AsString;

                // 调整tile索引
                tile -= UpperBound(midrId.ToArray(), tile);

                var asJsonObject = parsedChart[tile].AsJsonObject;
                if (asJsonObject != null) {
                    JsonObject ob = asJsonObject;

                    switch (eventType) {
                        case "SetSpeed":
                            if (o["speedType"].AsString == "Multiplier") {
                                bpm = o["bpmMultiplier"].AsNumber * bpm;
                            }
                            else if (o["speedType"].AsString == "Bpm") {
                                bpm = o["beatsPerMinute"].AsNumber * pitch;
                                //speedTypeIsBpm = true;
                            }

                            ob["bpm"] = bpm;
                            break;

                        case "Twirl":
                            ob["direction"] = -1;
                            break;

                        case "Pause":
                            ob["extraHold"] = o["duration"].AsNumber / 2.0;
                            break;

                        case "Hold":
                            ob["extraHold"] = o["duration"].AsNumber;
                            break;

                        case "MultiPlanet":
                            ob["MultiPlanet"] = o["planets"].AsString == "ThreePlanets" ? "1" : "0";
                            break;
                    }
                    parsedChart[tile] = ob;
                }
            }
        }


        double currentBPM = l.GetBPM() * pitch;
        int direction = 1;

        // 应用全局设置
        foreach (var t in parsedChart)
        {
            var asJsonObject = t.AsJsonObject;
            if (asJsonObject != null) {
                JsonObject ob = asJsonObject;
                // 方向处理
                if (ob["direction"].AsInteger == -1) {
                    direction *= -1;
                }

                ob["direction"] = direction;

                // BPM处理
                if (ob["bpm"].AsString == "unSet") {
                    ob["bpm"] = currentBPM;
                }
                else {
                    currentBPM = ob["bpm"].AsNumber;
                }
            }
        }

        List<double> noteTime = [];

        double curAngle = 0;
        double curTime = 0;
        bool isMultiPlanet = false;

        foreach (var chartValue in parsedChart)
        {
            var asJsonObject = chartValue.AsJsonObject;
            if (asJsonObject != null) 
            {
                JsonObject o = asJsonObject;
                curAngle = Fmod(curAngle - 180, 360);
                double curBPM = o["bpm"].AsNumber;
                double destAngle = o["angle"].AsNumber;

                double pAngle = Math.Abs(destAngle - curAngle) <= 0.001
                    ? 360
                    : Fmod(( curAngle - destAngle ) * o["direction"].AsInteger, 360);

                pAngle += o["extraHold"].AsNumber * 360;

                // 三球处理逻辑
                double angleTemp = pAngle;
                if (isMultiPlanet) {
                    pAngle = pAngle > 60 ? pAngle - 60 : pAngle + 300;
                }

                string? multiPlanet = o["MultiPlanet"].AsString;
                if (multiPlanet != "-1") {
                    isMultiPlanet = multiPlanet == "1";
                    pAngle = isMultiPlanet
                        ? ( pAngle > 60 ? pAngle - 60 : pAngle + 300 )
                        : angleTemp;
                }

                // 计算时间
                double deltaTime = AngleToTime(pAngle, curBPM);
                curTime += deltaTime;

                curAngle = destAngle;
                if (o["midr"].AsBoolean) {
                    curAngle += 180;
                }
                noteTime.Add(curTime);
            }
        }
        return noteTime;
    }

    private static double Fmod(double a, double b) => a - b * Math.Floor(a / b);

    private static int UpperBound(int[] arr, int value) {
        int left = 0;
        int right = arr.Length - 1;
        while (left <= right) {
            int mid = left + (right - left) / 2;
            if (arr[mid] >= value) {
                right = mid - 1;
            } else {
                left = mid + 1;
            }
        }
        return left;
    }

    private static double AngleToTime(double angle, double bpm)
    {
        return (angle / 180) * (60 / bpm) * 1000;
    }
    
    public class StartMacro
    {
        private static readonly ResourceLoader _resourceLoader = new ();
        private readonly List<int> _keys;
        private List<double> _noteTimes;
        private volatile bool _isRunning;
        private Thread? _workerThread;
        private int _keyIndex;
        private double _offset;
        private readonly Lock _syncLock = new Lock();
        private readonly List<double> intervals;
        public readonly Command? _command;

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

        public StartMacro(List<int> keys, List<double> noteTimes,bool cmd = false)
        {
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _noteTimes = noteTimes ?? throw new ArgumentNullException(nameof(noteTimes));
            intervals = noteTimes.Skip(1)
                .Select((time, index) => AppUtils.Max(time - _noteTimes[index],200))
                .ToList();
            intervals.Add(10);
            _command = cmd ? new Command() : null;
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
            _command?.Clear();
        }

        public void ChangeNoteTimes(List<double> noteTimes)
        {
            if (_isRunning) Stop();
            this._noteTimes = noteTimes;
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
                int events = 1;
                double startTime = GetCurrentTime();

                while (_isRunning && events < _noteTimes.Count)
                {
                    double currentTime = GetCurrentTime();
                    double timeMilliseconds = currentTime - startTime + _noteTimes[0];

                    // 批量处理符合条件的事件
                    while (events < _noteTimes.Count &&
                           (_noteTimes[events] + Offset) <= timeMilliseconds)
                    {
                        TriggerKeyPress(ref _keyIndex,events);
                        events++;
                    }
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        private double GetCurrentTime()
        {
            return AppUtils.currentTime();
        }
        
        private void TriggerKeyPress(ref int keyIndex,int currentIndex)
        {
            lock (_syncLock) // 保证线程安全的按键索引访问
            {
                if (keyIndex >= _keys.Count) keyIndex = 0;
                int keyCode = _keys[keyIndex];
                _command?.WriteLine($"{_resourceLoader.GetString("LevelUtils_CurrentKeyMessage")}: {KeyCodeConverter.GetKeyName(keyCode)}," +
                                    $"{_resourceLoader.GetString("LevelUtils_BpmMessage")}: {60000 / (_noteTimes[currentIndex] - _noteTimes[currentIndex - 1]):F4}," +
                                    $"{_resourceLoader.GetString("LevelUtils_NumberOfTilesMessage")}: {currentIndex}/{_noteTimes.Count -1}," +
                                    $"{_resourceLoader.GetString("LevelUtils_MisalignmentMessage")}: {_offset}ms");
                WindowsNativeAPI.PressKey(keyCode,intervals[currentIndex] - 5);
                keyIndex++;
            }
        }
    }
}