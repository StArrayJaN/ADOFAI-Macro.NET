using Json;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Diagnostics;

namespace ADOFAI_Macro.Source.Utils;

public static class LevelUtils
{
	public static HashSet<int> GetMidspinIndex(ADOFAI.Level l)
	{
		List<double> angleDataList = l.GetAngleData();
		
		var midspinIndex = new HashSet<int>();
		for (int i = 0; i < angleDataList.Count; i++)
		{
			if (Math.Abs(angleDataList[i] - 999) < 0.01)
			{
				midspinIndex.Add(i);
			}
		}
		return midspinIndex;
	}
    public static List<double> GetNoteTimes(ADOFAI.Level l)
    {
        List<double> angleDataList = l.GetAngleData();
        JsonArray levelEvents = l.actions;

        // 处理带有变速和旋转的中旋
        /*
        for (int i = 0; i < angleDataList.Count; i++)
        {
            if (Math.Abs(angleDataList[i] - 999) < 0.01)
            {
                if (l.HasEvent(i, "SetSpeed"))
                {
                    int index = l.GetEventIndex(i, "SetSpeed");
                    JsonObject? a = levelEvents[index].AsJsonObject;
                    if (a != null)
                    {
                        a["floor"] = a["floor"].AsInteger + 1;
                    }
                }
                else if (l.HasEvent(i, "Twirl"))
                {
                    int index = l.GetEventIndex(i, "Twirl");
                    JsonObject? a = levelEvents[index].AsJsonObject;
                    if (a != null)
                    {
                        a["floor"] = a["floor"].AsInteger + 1;
                    }
                }
            }
        }
*/
        JsonArray parsedChart = new JsonArray();
        int midrCount = 0;
        List<int> midrId = new List<int>();

        // 初步处理轨道数据
        for (int i = 0; i < angleDataList.Count; i++)
        {
            double angleData = angleDataList[i];
            if (Math.Abs(angleData - 999) < 0.01)
            {/*
                midrCount++;
                JsonObject? temp = parsedChart[i - midrCount].AsJsonObject;
                if (temp != null)
                {
                    temp["midr"] = "true";
                    parsedChart[i - midrCount] = temp;
                }
                midrId.Add(i - 1);*/
                JsonObject temp = new JsonObject()
                    .Add("angle", Fmod(angleDataList[i - 1] + 180, 360))
                    .Add("bpm", "unSet")
                    .Add("direction", 0)
                    .Add("extraHold", 0)
                    .Add("midr", true)
                    .Add("MultiPlanet", "-1");

                parsedChart.Add(temp);

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

        double bpm = l.GetBPM();
        double pitch = l.GetPitch() / 100.0;

        // 处理事件数据
        foreach (var eventValue in levelEvents)
        {
            JsonObject? o = eventValue.AsJsonObject;
            if (o != null)
            {
                int tile = o["floor"].AsInteger;
                string? eventType = o["eventType"].AsString;

                // 调整tile索引
                //tile -= UpperBound(midrId.ToArray(), tile);

                var asJsonObject = parsedChart[tile].AsJsonObject;
                if (asJsonObject != null)
                {
                    JsonObject ob = asJsonObject;

                    switch (eventType)
                    {
                        case "SetSpeed":
                            if (o["speedType"].AsString == "Multiplier")
                            {
                                bpm = o["bpmMultiplier"].AsNumber * bpm;
                            }
                            else if (o["speedType"].AsString == "Bpm")
                            {
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
                        
                        case "FreeRoam":
                            ob["extraHold"] = (o["duration"].AsNumber - 1) / 2.0;
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
            if (asJsonObject != null)
            {
                JsonObject ob = asJsonObject;
                // 方向处理
                if (ob["direction"].AsInteger == -1)
                {
                    direction *= -1;
                }

                ob["direction"] = direction;

                // BPM处理
                if (ob["bpm"].AsString == "unSet")
                {
                    ob["bpm"] = currentBPM;
                }
                else
                {
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

                double pAngle = (Fmod(destAngle - curAngle, 360) <= 0.001 || Fmod(destAngle - curAngle, 360) >= 359.999)
                    ? 360
                    : Fmod((curAngle - destAngle) * o["direction"].AsInteger, 360);

                pAngle += o["extraHold"].AsNumber * 360;

                // 三球处理逻辑
                double angleTemp = pAngle;
                if (isMultiPlanet)
                {
                    pAngle = pAngle > 60 ? pAngle - 60 : pAngle + 300;
                }

                string? multiPlanet = o["MultiPlanet"].AsString;
                if (multiPlanet != "-1")
                {
                    isMultiPlanet = multiPlanet == "1";
                    pAngle = isMultiPlanet
                        ? (pAngle > 60 ? pAngle - 60 : pAngle + 300)
                        : angleTemp;
                }

                // 计算时间
                double deltaTime = o["midr"].AsBoolean ? 0 : AngleToTime(pAngle, curBPM);
                curTime += deltaTime;

                curAngle = destAngle;
                /*
                if (o["midr"].AsBoolean)
                {
                    curAngle += 180;
                }*/
                noteTime.Add(curTime);
            }
        }
        Debug.WriteLine(noteTime.Count);
        return noteTime;
    }
    private static double Fmod(double a, double b) => a - b * Math.Floor(a / b);

    private static int UpperBound(int[] arr, int value)
    {
        int left = 0;
        int right = arr.Length - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (arr[mid] >= value)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }
        return left;
    }

    private static double AngleToTime(double angle, double bpm)
    {
        return (angle / 180) * (60 / bpm) * 1000;
    }

    
}