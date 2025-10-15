using System.Collections.Generic;
using System.Windows.Input;

namespace YuYue.Models;

/// <summary>
/// 热键配置
/// </summary>
public class HotkeyConfig
{
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }
    public bool IsEnabled { get; set; } = true;
    
    public string DisplayText => $"{Modifiers}+{Key}";
    
    public static List<HotkeyConfig> GetDefaultConfigs()
    {
        return new List<HotkeyConfig>
        {
            new() { Action = "ShowHide", Description = "显示/隐藏窗口", Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.Y },
            new() { Action = "NextPage", Description = "下一页", Modifiers = ModifierKeys.None, Key = Key.PageDown },
            new() { Action = "PreviousPage", Description = "上一页", Modifiers = ModifierKeys.None, Key = Key.PageUp },
            new() { Action = "NextChapter", Description = "下一章", Modifiers = ModifierKeys.Control, Key = Key.Down },
            new() { Action = "PreviousChapter", Description = "上一章", Modifiers = ModifierKeys.Control, Key = Key.Up },
            new() { Action = "AddBookmark", Description = "添加书签", Modifiers = ModifierKeys.Control, Key = Key.D },
            new() { Action = "ToggleImmersive", Description = "沉浸模式", Modifiers = ModifierKeys.None, Key = Key.F11 },
            new() { Action = "ToggleCamouflage", Description = "伪装模式", Modifiers = ModifierKeys.Control | ModifierKeys.Shift, Key = Key.C },
            new() { Action = "SaveProgress", Description = "保存进度", Modifiers = ModifierKeys.Control, Key = Key.S },
            new() { Action = "BackToBookshelf", Description = "返回书架", Modifiers = ModifierKeys.None, Key = Key.Escape }
        };
    }
}
