using System.Diagnostics;
using System.Reflection;
using System.Text;
using LightJson;
using LightJson.Serialization;

namespace ADOFAI_Macro.Source.Utils;

public class ADOFAI
{
    public ADOFAI()
    {
        // TODO: 如果需要向用户显示信息就把这个删掉，不需要就把注释删掉
        installDir = SteamPath.SteamPath.Find("977950") ?? string.Empty;
        string name = new DirectoryInfo(installDir).Name;
        exePath = $"{installDir}\\{name}.exe";
        mainDll = Assembly.LoadFrom($"{installDir}\\{name}_Data\\Managed\\Assembly-CSharp.dll");
    }

    public string installDir { get; private set; }

    public string exePath { get; private set; }

    public Assembly mainDll { get; private set; }

    public JsonObject? levelJson { get; private set; }

    public MethodBase? GetMethod(string className, string methodName, params Type[] parameterTypes)
    {
        return mainDll.GetType(className)?.GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public, null,
            parameterTypes, null);
    }

    public Process RunProcess()
    {
        return Process.Start(exePath);
    }

    public Process FindOrRunProcess(out bool isNewSession)
    {
        Process[] processList = Process.GetProcesses();
        foreach (Process process in processList)
        {
            if (process.ProcessName == new FileInfo(exePath).Name.Replace(".exe", ""))
            {
                isNewSession = false;
                return process;
            }
        }

        isNewSession = true;
        return RunProcess();
    }

    public Process FindOrRunProcess()
    {
        return FindOrRunProcess(out _);
    }

    public bool IsWindowActive()
    {
        return FindOrRunProcess().MainWindowHandle == WindowsNative.GetForegroundWindow();
    }

    public Level InitLevel(string levelPath)
    {
        string str = Files.ReadFileIfException(levelPath);
        str = str.Substring(str.IndexOf('{'), str.Length - 1)
            .Replace(" ", "")
            .Replace(",}", "}");
        var strList = str.Split('\n').ToList(); 
        try
        {
            JsonValue.Parse(str);
        }
        catch (JsonParseException e)
        {
            if (e.Message.Contains("Parser expected '\"',position"))
            {
                string[] position = e.Message[(e.Message.IndexOf('{') + 1)..].Replace(" ","").Replace("]","").Split(",");

                int[] pos = [int.Parse(position[0]),int.Parse(position[1])];
                strList[pos[0] -1] = strList[pos[0] -1].Replace(",", "");
            }

            if (e.Message.Contains("The parser encountered an invalid or unexpected character."))
            {
                string[] position = e.Message[(e.Message.IndexOf('[') + 1)..].Replace(" ","").Replace("]","").Split(",");

                int[] pos = [int.Parse(position[0]),int.Parse(position[1])];
                strList[pos[0] -1] = strList[pos[0] -1].Replace("]","],");
            }
        }

        levelJson = JsonValue.Parse(toString(strList));
        return new Level(levelJson);

        string toString(List<string> strs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in strs)
            {
                sb.AppendLine(s);
            }
            return RemoveControlCharacters(sb.ToString());
        }
    }

    /// <summary>
    /// 移除字符串中所有Unicode值小于\u0020的控制字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>清理后的字符串</returns>
    public static string RemoveControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        // 使用LINQ过滤所有大于等于\u0020的字符
        return new string(input
            .Where(c => c >= '\u0020')
            .ToArray());
    }
    public class Level(JsonObject jsonObject)
    {
        JsonObject json = jsonObject;
        public readonly JsonArray actions = jsonObject["actions"];

        public List<double> GetAngleData()
        {
            if (json.ContainsKey("angleData"))
            {
                return json["angleData"].AsJsonArray?.Select(x => x.AsNumber).ToList() ?? new();
            }
            return json["pathData"]
                    .AsString?
                    .ToCharArray()
                    .Select(c => TileAngle.AngleCharMap[c].Angle)
                    .ToList() ?? new();
        }

        public JsonObject? GetEvent(int floor,string eventName)
        {
            foreach (var evnt in actions)
            {
                if (evnt["floor"] == floor && evnt["eventType"] == eventName)
                {
                    return evnt;
                }
            }
            return null;
        }
        
        public bool HasEvent(int floor,string eventName) => GetEvent(floor,eventName) != null;

        public double GetBPM()
        {
            return json["settings"]["bpm"].AsNumber;
        }
        
        public int GetPitch() => json["settings"]["pitch"].AsInteger;

        public int GetEventIndex(int floor, string eventName)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i]["floor"] == floor && actions[i]["eventType"] == eventName)
                {
                    return i;
                }
            }
            return -1;
        }
        
        public class TileAngle
        {
            public static readonly TileAngle _0 = new TileAngle('R', 0, false);
            public static readonly TileAngle _15 = new TileAngle('p', 15, false);
            public static readonly TileAngle _30 = new TileAngle('J', 30, false);
            public static readonly TileAngle _45 = new TileAngle('E', 45, false);
            public static readonly TileAngle _60 = new TileAngle('T', 60, false);
            public static readonly TileAngle _75 = new TileAngle('o', 75, false);
            public static readonly TileAngle _90 = new TileAngle('U', 90, false);
            public static readonly TileAngle _105 = new TileAngle('q', 105, false);
            public static readonly TileAngle _120 = new TileAngle('G', 120, false);
            public static readonly TileAngle _135 = new TileAngle('Q', 135, false);
            public static readonly TileAngle _150 = new TileAngle('H', 150, false);
            public static readonly TileAngle _165 = new TileAngle('W', 165, false);
            public static readonly TileAngle _180 = new TileAngle('L', 180, false);
            public static readonly TileAngle _195 = new TileAngle('x', 195, false);
            public static readonly TileAngle _210 = new TileAngle('N', 210, false);
            public static readonly TileAngle _225 = new TileAngle('Z', 225, false);
            public static readonly TileAngle _240 = new TileAngle('F', 240, false);
            public static readonly TileAngle _255 = new TileAngle('V', 255, false);
            public static readonly TileAngle _270 = new TileAngle('D', 270, false);
            public static readonly TileAngle _285 = new TileAngle('Y', 285, false);
            public static readonly TileAngle _300 = new TileAngle('B', 300, false);
            public static readonly TileAngle _315 = new TileAngle('C', 315, false);
            public static readonly TileAngle _330 = new TileAngle('M', 330, false);
            public static readonly TileAngle _345 = new TileAngle('A', 345, false);
            public static readonly TileAngle _5 = new TileAngle('5', 108, true);
            public static readonly TileAngle _6 = new TileAngle('6', 252, true);
            public static readonly TileAngle _7 = new TileAngle('7', 900.0 / 7.0, true);
            public static readonly TileAngle _8 = new TileAngle('8', 360 - 900.0 / 7.0, true);
            public static readonly TileAngle R60 = new TileAngle('t', 60, true);
            public static readonly TileAngle R120 = new TileAngle('h', 120, true);
            public static readonly TileAngle R240 = new TileAngle('j', 240, true);
            public static readonly TileAngle R300 = new TileAngle('y', 300, true);
            public static readonly TileAngle NONE = new TileAngle('!', 999, true);

            public static readonly Dictionary<char, TileAngle> AngleCharMap = new Dictionary<char, TileAngle>();

            static TileAngle()
            {
                foreach (var field in typeof(TileAngle).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(TileAngle))
                    {
                        var tileAngle = (TileAngle?)field.GetValue(null);
                        if (tileAngle != null)
                        {
                            AngleCharMap[tileAngle.CharCode] = tileAngle;
                        }
                    }
                }
            }

            private TileAngle(char charCode, double angle, bool relative)
            {
                CharCode = charCode;
                Angle = angle;
                Relative = relative;
            }

            private char CharCode { get; }
            public double Angle { get; }
            public bool Relative;
        }
    }
}