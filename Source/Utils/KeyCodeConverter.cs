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
    
     public static readonly Dictionary<string, int> KeyMap = new Dictionary<string, int>
    {
        // 字母键（ASCII码）
        {"A", 65}, {"B", 66}, {"C", 67}, {"D", 68}, {"E", 69},
        {"F", 70}, {"G", 71}, {"H", 72}, {"I", 73}, {"J", 74},
        {"K", 75}, {"L", 76}, {"M", 77}, {"N", 78}, {"O", 79},
        {"P", 80}, {"Q", 81}, {"R", 82}, {"S", 83}, {"T", 84},
        {"U", 85}, {"V", 86}, {"W", 87}, {"X", 88}, {"Y", 89},
        {"Z", 90},
        
        // 功能键（F1-F12）
        {"F1", 112}, {"F2", 113}, {"F3", 114}, {"F4", 115}, {"F5", 116},
        {"F6", 117}, {"F7", 118}, {"F8", 119}, {"F9", 120}, {"F10", 121},
        {"F11", 122}, {"F12", 123},
        // 符号键（主键盘区）
        {"~", 192}, {"`", 192}, // ~ 和 ` 共用同一个键码
        {"!", 49}, {"1", 49},   // ! 和 1 共用同一个键码
        {"@", 50}, {"2", 50},   // @ 和 2 共用同一个键码
        {"#", 51}, {"3", 51},   // # 和 3 共用同一个键码
        {"$", 52}, {"4", 52},   // $ 和 4 共用同一个键码
        {"%", 53}, {"5", 53},   // % 和 5 共用同一个键码
        {"^", 54}, {"6", 54},   // ^ 和 6 共用同一个键码
        {"&", 55}, {"7", 55},   // & 和 7 共用同一个键码
        {"*", 56}, {"8", 56},   // * 和 8 共用同一个键码
        {"(", 57}, {"9", 57},   // ( 和 9 共用同一个键码
        {")", 48}, {"0", 48},   // ) 和 0 共用同一个键码
        {"_", 189}, {"-", 189}, // _ 和 - 共用同一个键码
        {"+", 187}, {"=", 187}, // + 和 = 共用同一个键码
        {"[", 219}, {"{", 219}, // [ 和 { 共用同一个键码
        {"]", 221}, {"}", 221}, // ] 和 } 共用同一个键码
        {"\\", 220}, {"|", 220}, // \ 和 | 共用同一个键码
        {";", 186}, {":", 186}, // ; 和 : 共用同一个键码
        {"'", 222}, {"\"", 222}, // ' 和 " 共用同一个键码
        {",", 188}, {"<", 188}, // , 和 < 共用同一个键码
        {".", 190}, {">", 190}, // . 和 > 共用同一个键码
        {"/", 191}, {"?", 191}, // / 和 ? 共用同一个键码

        // 特殊键
        {"Enter", 13}, {"Tab", 9}, {"Escape", 27}, {"Space", 32},
        {"Backspace", 8}, {"Delete", 46}, {"Insert", 45},
        {"Home", 36}, {"End", 35}, {"PageUp", 33}, {"PageDown", 34},
        {"Up", 38}, {"Down", 40}, {"Left", 37}, {"Right", 39},
        {"CapsLock", 20}, {"NumLock", 144}, {"ScrollLock", 145},
        {"PrintScreen", 44}, {"Pause", 19},

        // 小键盘数字键
        {"Numpad0", 96}, {"Numpad1", 97}, {"Numpad2", 98}, {"Numpad3", 99}, {"Numpad4", 100},
        {"Numpad5", 101}, {"Numpad6", 102}, {"Numpad7", 103}, {"Numpad8", 104}, {"Numpad9", 105},
        {"NumpadMultiply", 106}, {"NumpadAdd", 107}, {"NumpadSubtract", 109}, {"NumpadDecimal", 110},
        {"NumpadDivide", 111},

        // 其他特殊键
        {"Shift", 16}, {"Control", 17}, {"Alt", 18}, {"Win", 91},
        {"ContextMenu", 93}, {"LeftShift", 160}, {"RightShift", 161},
        {"LeftControl", 162}, {"RightControl", 163}, {"LeftAlt", 164}, {"RightAlt", 165}
    };
    public static readonly Dictionary<int, string> ReversedKeyMap = new Dictionary<int, string>();

    static KeyCodeConverter()
    {
        foreach (var kvp in KeyMap)
        {
            ReversedKeyMap[kvp.Value] = kvp.Key;
        }
    }
    
    
    // 根据按键名称获取键码
    public static int GetKeyCode(string keyName)
    {
        if (KeyMap.TryGetValue(keyName, out int keyCode))
        {
            return keyCode;
        }
        throw new ArgumentException($"Key '{keyName}' not found in KeyMap.");
    }

    // 根据键码获取按键名称
    public static string GetKeyName(int keyCode)
    {
        return ReversedKeyMap[keyCode];
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