using ClickFlow.UI.Controls;

namespace ClickFlow.UI;

internal static class L
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["轻录"] = "ClickFlow",
        ["轻录 ClickFlow"] = "ClickFlow",
        ["ClickFlow  ·  把重复操作录一次"] = "ClickFlow · record it once",
        ["设置 · 轻录"] = "Settings · ClickFlow",
        ["帮助 · 轻录"] = "Help · ClickFlow",
        ["宏录制"] = "Macro",
        ["自动连点"] = "Auto click",
        ["待机"] = "Idle",
        ["回放选中"] = "Play",
        ["停止"] = "Stop",
        ["回放设置"] = "Playback",
        ["回放速度"] = "Speed",
        ["准备时间"] = "Delay",
        ["循环次数"] = "Loops",
        ["录制鼠标移动轨迹"] = "Record mouse movement",
        ["回放后恢复鼠标位置"] = "Restore cursor after playback",
        ["点击位置"] = "Click position",
        ["当前鼠标位置"] = "Current position",
        ["固定坐标"] = "Fixed position",
        ["获取当前鼠标坐标"] = "Pick current cursor",
        ["连点参数"] = "Auto click options",
        ["鼠标键"] = "Button",
        ["左键"] = "Left",
        ["右键"] = "Right",
        ["中键"] = "Middle",
        ["点击间隔"] = "Interval",
        ["点击次数"] = "Count",
        ["随机偏移"] = "Random offset",
        ["开始连点"] = "Start clicking",
        ["F4 开始 / 停止连点  ·  F5 捕获当前坐标  ·  F3 急停\n次数设为 0 时持续点击，直到手动停止。"] =
            "F4 start / stop auto click · F5 pick position · F3 stop\nCount 0 clicks until stopped.",
        ["连点开始前会短暂准备，让目标窗口获得焦点；F3 可随时停止。"] =
            "A short countdown focuses the target window; F3 stops at any time.",
        ["还没有录制记录\n\n按 F1 开始录制，停止后会自动保存在这里"] =
            "No recordings yet\n\nPress F1 to record; it will be saved here.",
        ["按 F1 开始录制"] = "Press F1 to record",
        ["录制历史"] = "History",
        ["本地保存"] = "Saved locally",
        ["导入"] = "Import",
        ["文件夹"] = "Folder",
        ["把重复点击交给轻录"] = "Let ClickFlow handle the clicks",
        ["就绪"] = "Ready",
        ["开始连点"] = "Start clicking",
        ["本地运行 · 不上传数据"] = "Local only · no upload",
        ["关闭"] = "Off",
        ["3 秒"] = "3 sec",
        ["5 秒"] = "5 sec",
        ["次"] = "",
        ["设置"] = "Settings",
        ["使用帮助"] = "Help",
        ["外观"] = "Appearance",
        ["通用"] = "General",
        ["数据"] = "Data",
        ["关于与更新"] = "About & updates",
        ["背景配色"] = "Theme",
        ["界面字号"] = "Font size",
        ["重启后生效"] = "Restart to apply",
        ["自定义颜色…"] = "Custom color…",
        ["启动页面"] = "Start page",
        ["启动时检查更新"] = "Check for updates on startup",
        ["界面语言"] = "Language",
        ["简体中文"] = "简体中文",
        ["打开数据文件夹"] = "Open data folder",
        ["检查更新"] = "Check for updates",
        ["打开发布页"] = "Open releases",
        ["当前版本"] = "Current version",
        ["正在检查…"] = "Checking…",
        ["取消"] = "Cancel",
        ["保存"] = "Save",
        ["小"] = "Small",
        ["标准"] = "Standard",
        ["大"] = "Large",
        ["深空黑"] = "Deep Space",
        ["石墨灰"] = "Graphite",
        ["午夜蓝"] = "Midnight",
        ["墨绿"] = "Forest",
        ["酒红"] = "Wine",
        ["暖褐"] = "Amber",
        ["紫夜"] = "Violet",
        ["深海"] = "Ocean",
        ["纯白"] = "Pure White",
        ["暖白"] = "Warm White",
        ["极光渐变"] = "Aurora Gradient",
        ["晨曦渐变"] = "Dawn Gradient",
        ["薄荷渐变"] = "Mint Gradient",
        ["选择背景配色和界面字号。支持纯白、浅色和渐变主题。"] = "Choose a theme and font size. Light and gradient themes are included.",
        ["启动时打开哪个页面，以及录制和回放时的默认行为。"] = "Choose the start page and default recording/playback behavior.",
        ["录制记录默认保存在程序旁的 data 文件夹。删除记录会同时删除对应文件。"] = "Recordings live in the data folder next to the app. Deleting a recording deletes its file too.",
        ["轻录 ClickFlow · 本地录制与回放工具"] = "ClickFlow · local recording and playback",
        ["备份或迁移时，复制整个 data 文件夹即可。"] = "To back up or move, copy the whole data folder.",
        ["检查是否有新版本。"] = "Check whether a new version is available.",
        ["外观、通用行为和版本更新都集中在这里。"] = "Appearance, general behavior and updates in one place.",
        ["回放按原始时间轴进行，速度按 0.25× 步进（0.25–4×）。\n停止时会自动松开所有按住的键。"] =
            "Playback follows the original timeline; speed steps by 0.25× (0.25–4×).\nHeld keys are released when playback stops.",
        ["点击、拖动、滚动或按键，按 F1 结束录制"] = "Click, drag, scroll or type; press F1 to stop",
        ["按下 F1 开始录制，或打开一个已有的宏文件"] = "Press F1 to record, or open an existing macro",
        ["使用帮助 · 轻录"] = "Help · ClickFlow",
        ["使用帮助"] = "Help",
        ["三段式操作：录制 → 调整 → 回放。所有内容只保存在本机。"] = "Record → adjust → replay. Everything stays on this PC.",
        ["录制小贴士"] = "Recording tips",
        ["• 倒计时结束后，轻录会自动最小化，把前台让给目标窗口。\n• 拖动鼠标会被记录成一条“拖动/滑动”操作，轨迹和耗时可原样回放。\n• 滚轮、右键、中键、键盘组合键都可以录制。\n• 回放速度可设为 0.5× ~ 4×，循环次数最多 999 次。\n• 每次停止录制都会自动保存一份 last-session.cfmacro，重新打开轻录可恢复。"] =
            "• After the countdown, ClickFlow minimizes and gives focus back to the target window.\n• Dragging is recorded as a drag/swipe with its path and duration.\n• Wheel, right/middle click and keyboard shortcuts are supported.\n• Playback speed is adjustable; loops can go up to 999.\n• Every finished recording is saved to the recording history.",
        ["使用边界"] = "Limits",
        ["轻录使用 Windows 标准输入注入，无法操作以管理员权限运行的窗口、UAC 安全桌面、锁屏界面，\n也无法绕过部分游戏或安全软件的反作弊限制。需要操作管理员窗口时，请以管理员身份启动轻录。\n如果 F1–F5 被其它软件占用，轻录会自动改用 Ctrl+F1–F5；回放时按 F3/Esc 或点击顶部急停。\n每次录制都会自动进入录制历史；删除记录会同时删除本地文件。"] =
            "ClickFlow uses standard Windows input injection. It cannot operate elevated windows, the UAC secure desktop or the lock screen, and some games/security tools block simulated input. Run ClickFlow as administrator when needed.\nIf F1–F5 are occupied, ClickFlow automatically uses Ctrl+F1–F5. Press F3/Esc or click Stop above during playback.\nEvery recording is saved to History; deleting it also deletes the local file.",
        ["全局快捷键"] = "Global hotkeys",
        ["开始 / 停止录制"] = "Start / stop recording",
        ["开始 / 停止回放"] = "Start / stop playback",
        ["紧急停止（录制、回放、连点全部停止）"] = "Emergency stop (recording, playback, auto click)",
        ["开始 / 停止自动连点"] = "Start / stop auto click",
        ["捕获当前鼠标坐标"] = "Capture current cursor position",
        ["打开使用帮助"] = "Open help",
        ["知道了"] = "OK"
    };

    public static bool IsEnglish { get; private set; }

    public static void SetLanguage(string? language)
    {
        IsEnglish = string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase);
    }

    public static string T(string chinese, string english) => IsEnglish ? english : chinese;

    public static string S(string chinese)
    {
        if (!IsEnglish)
        {
            return chinese;
        }

        return English.TryGetValue(chinese, out var translated) ? translated : chinese;
    }

    public static void Apply(Control root)
    {
        if (!string.IsNullOrEmpty(root.Text))
        {
            root.Text = S(root.Text);
        }

        if (root is SegmentedControl segmented)
        {
            segmented.Items = segmented.Items.Select(S).ToArray();
        }

        if (root is NumberStepper stepper && IsEnglish && stepper.Suffix == "次")
        {
            stepper.Suffix = string.Empty;
        }

        foreach (Control child in root.Controls)
        {
            Apply(child);
        }
    }
}
