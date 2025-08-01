using System.Diagnostics;
using System.Text;
using Json;
using WinRT;

namespace ADOFAI_Macro.Source.Utils;

public class AppData
{
    public static AppData instance = new();
    private readonly Dictionary<string,string> dataMap = new ();
    private readonly string dataPath = Directory.GetCurrentDirectory() + "\\data.properties";
    private AppData()
    {
        if (!File.Exists(dataPath))
        {
            File.Create(dataPath).Close();
        }
        else
        {
            string data = File.ReadAllText(dataPath);
            foreach (var item in data.Split("\r\n"))
            {
                string[] split = item.Split('=');
                if (split.Length == 2)
                {
                    dataMap[split[0]] = split[1];
                }
            }
        }
    }

    public void Write(string key,object val)
    {
        var valString = val.ToString();
        if (valString != null) 
        {
            dataMap[key] = valString;
        }
        Save();
    }

    public string Read(string key, string def)
    {
        if (dataMap.ContainsKey(key))
        {
            return dataMap[key];
        }
        return def;
    }

    public void Save()
    {
        StringBuilder stringBuilder = new();
        foreach (var item in dataMap)
        {
            stringBuilder.AppendLine($"{item.Key}={item.Value}");
        }
        File.WriteAllText(dataPath, stringBuilder.ToString());
    }
}