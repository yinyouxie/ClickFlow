namespace ClickFlow.Core;

internal static class KeyNames
{
    public const int VkShift = 0x10;
    public const int VkControl = 0x11;
    public const int VkAlt = 0x12;
    public const int VkLWin = 0x5B;
    public const int VkRWin = 0x5C;

    private static readonly Dictionary<int, string> Names = new()
    {
        [0x08] = "Backspace",
        [0x09] = "Tab",
        [0x0C] = "Clear",
        [0x0D] = "Enter",
        [0x10] = "Shift",
        [0x11] = "Ctrl",
        [0x12] = "Alt",
        [0x13] = "Pause",
        [0x14] = "CapsLock",
        [0x1B] = "Esc",
        [0x20] = "Space",
        [0x21] = "PageUp",
        [0x22] = "PageDown",
        [0x23] = "End",
        [0x24] = "Home",
        [0x25] = "←",
        [0x26] = "↑",
        [0x27] = "→",
        [0x28] = "↓",
        [0x2C] = "PrintScreen",
        [0x2D] = "Insert",
        [0x2E] = "Delete",
        [0x5B] = "Win",
        [0x5C] = "Win",
        [0x5D] = "Menu",
        [0x6A] = "Num *",
        [0x6B] = "Num +",
        [0x6C] = "Num ,",
        [0x6D] = "Num -",
        [0x6E] = "Num .",
        [0x6F] = "Num /",
        [0x90] = "NumLock",
        [0x91] = "ScrollLock",
        [0xA0] = "Shift",
        [0xA1] = "Shift",
        [0xA2] = "Ctrl",
        [0xA3] = "Ctrl",
        [0xA4] = "Alt",
        [0xA5] = "Alt",
        [0xBA] = ";",
        [0xBB] = "=",
        [0xBC] = ",",
        [0xBD] = "-",
        [0xBE] = ".",
        [0xBF] = "/",
        [0xC0] = "`",
        [0xDB] = "[",
        [0xDC] = "\\",
        [0xDD] = "]",
        [0xDE] = "'",
        [0xE2] = "OEM 102",
        [0xE5] = "IME"
    };

    public static bool IsModifier(int vk) =>
        vk is VkShift or VkControl or VkAlt or VkLWin or VkRWin
            or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;

    public static bool IsHotkey(int vk) => vk is 0x70 or 0x71 or 0x72 or 0x73 or 0x74;

    public static string GetName(int vk)
    {
        if (Names.TryGetValue(vk, out var name))
        {
            return name;
        }

        if (vk is >= 0x41 and <= 0x5A)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x30 and <= 0x39)
        {
            return ((char)vk).ToString();
        }

        if (vk is >= 0x60 and <= 0x69)
        {
            return $"Num {vk - 0x60}";
        }

        if (vk is >= 0x70 and <= 0x87)
        {
            return $"F{vk - 0x6F}";
        }

        return vk == 0 ? "键盘" : $"VK {vk:X2}";
    }

    public static string MouseName(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Left => "左键",
        MouseButtonKind.Right => "右键",
        MouseButtonKind.Middle => "中键",
        MouseButtonKind.X1 => "侧键 1",
        MouseButtonKind.X2 => "侧键 2",
        _ => "鼠标"
    };

    public static string MouseVerb(MouseButtonKind button) => button switch
    {
        MouseButtonKind.Left => "单击",
        MouseButtonKind.Right => "右键单击",
        MouseButtonKind.Middle => "中键单击",
        MouseButtonKind.X1 => "侧键 1 单击",
        MouseButtonKind.X2 => "侧键 2 单击",
        _ => "单击"
    };
}
