using Windows.System;

namespace ADOFAI_Macro.Source.Utils;

using System;
using System.Runtime.InteropServices;

public class KeyCodeConverter
{
    // 导入Windows API
    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    // 虚拟键码常量（完整列表参考WinUser.h）
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12; // ALT键

    /// <summary>
    /// 获取字符对应的虚拟键码和修饰键状态
    /// </summary>
    /// <param name="character">要转换的字符</param>
    /// <returns>
    /// Tuple包含：
    /// Item1 - 主虚拟键码
    /// Item2 - 是否需要Shift
    /// Item3 - 是否需要Ctrl
    /// Item4 - 是否需要Alt
    /// </returns>

    public static Dictionary<int, string> keys = new();

    public static void InitKeys()
    {
        if (keys.Count > 0) return;
        int[] values = Enum.GetValues<VirtualKey>().Select(a => (int)a).ToArray();
        string[] names = Enum.GetNames<VirtualKey>().Select(a => a.ToString()).ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            keys[values[i]] = names[i];
        }
    }
    public static Tuple<byte, bool, bool, bool> GetKeyCodeTuple(char character)
    {
        short result = VkKeyScan(character);
        
        if (result == -1)
            throw new ArgumentException($"无法转换字符 '{character}' (0x{(int)character:X4})");

        byte virtualKey = (byte)(result & 0xFF);
        byte shiftState = (byte)((result >> 8) & 0xFF);

        bool shift = (shiftState & 1) != 0;
        bool ctrl = (shiftState & 2) != 0;
        bool alt = (shiftState & 4) != 0;

        return Tuple.Create(virtualKey, shift, ctrl, alt);
    }

    /// <summary>
    /// 获取字符的十六进制键码表示
    /// </summary>
    public static string GetHexKeyCode(char character)
    {
        var keyInfo = GetKeyCodeTuple(character);
        return $"0x{keyInfo.Item1:X2}";
    }

    public static string GetKeyString(int keyCode)
    {
        InitKeys();
        return keys[keyCode];
    }
    
    public static int GetKeyCode(char character)
    {
        var keyInfo = GetKeyCodeTuple(character);
        return keyInfo.Item1;
    }

    /// <summary>
    /// 获取带修饰键状态的完整键码表示
    /// </summary>
    public static string GetFullKeyCodeString(char character)
    {
        var keyInfo = GetKeyCodeTuple(character);
        string modifiers = string.Empty;
        
        if (keyInfo.Item2) modifiers += "Shift + ";
        if (keyInfo.Item3) modifiers += "Ctrl + ";
        if (keyInfo.Item4) modifiers += "Alt + ";

        return $"{modifiers}0x{keyInfo.Item1:X2}";
    }
}