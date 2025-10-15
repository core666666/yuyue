using System.Text.Json.Serialization;

namespace YuYue.Models;

/// <summary>
/// 用户阅读偏好设置
/// </summary>
public class ReadingPreferences
{
    // 字体设置
    public double FontSize { get; set; } = 16;
    public double LineHeight { get; set; } = 26;
    public double ParagraphSpacing { get; set; } = 8;
    public bool EnableFirstLineIndent { get; set; } = true;
    public double FirstLineIndent { get; set; } = 32;
    
    // 排版设置
    public int ColumnCount { get; set; } = 1;
    public int PageSize { get; set; } = 1200;
    
    // 主题设置
    public string SelectedThemeName { get; set; } = "日间模式";
    public bool UseDarkTheme { get; set; } = false;
    
    // 自定义主题
    public CustomTheme? CustomTheme { get; set; }
    
    // 自动翻页
    public bool AutoPageEnabled { get; set; } = false;
    public int AutoPageInterval { get; set; } = 10;
    
    // 界面设置
    public bool ImmersiveModeEnabled { get; set; } = false;
    public bool ShowChapterPanel { get; set; } = false;
    public bool ShowBookmarkPanel { get; set; } = false;
    public bool ShowStatisticsPanel { get; set; } = false;
    
    // 窗口设置
    public bool WindowTopmost { get; set; } = false;
    public double WindowOpacity { get; set; } = 0.95;
    public bool BorderlessMode { get; set; } = true;
    
    // 阅读习惯
    public bool AutoResumeLastBook { get; set; } = true;
    public bool AutoSaveProgress { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 30; // 秒
    
    // 统计设置
    public bool EnableReadingTimer { get; set; } = true;
    public bool ShowReadingStatistics { get; set; } = true;
    
    // 快捷键设置
    public KeyBindings KeyBindings { get; set; } = new();
    
    // 热键配置
    public List<HotkeyConfig> HotkeyConfigs { get; set; } = HotkeyConfig.GetDefaultConfigs();
    
    // 开机自启动
    public bool AutoStartEnabled { get; set; } = false;
}

/// <summary>
/// 自定义主题
/// </summary>
public class CustomTheme
{
    public string Name { get; set; } = "自定义主题";
    public string BackgroundColor { get; set; } = "#FBFCFE";
    public string ForegroundColor { get; set; } = "#2C3E50";
}

/// <summary>
/// 快捷键绑定
/// </summary>
public class KeyBindings
{
    public string NextPage { get; set; } = "PageDown";
    public string PreviousPage { get; set; } = "PageUp";
    public string NextChapter { get; set; } = "Ctrl+Down";
    public string PreviousChapter { get; set; } = "Ctrl+Up";
    public string AddBookmark { get; set; } = "Ctrl+D";
    public string ToggleImmersive { get; set; } = "F11";
    public string ToggleTimer { get; set; } = "Ctrl+P";
    public string SaveProgress { get; set; } = "Ctrl+S";
    public string BackToBookshelf { get; set; } = "Escape";
}

/// <summary>
/// 阅读统计数据
/// </summary>
public class ReadingStatistics
{
    public int TotalReadingMinutes { get; set; }
    public int TodayReadingMinutes { get; set; }
    public int BooksRead { get; set; }
    public int TotalChaptersRead { get; set; }
    public DateTime LastReadingDate { get; set; }
    public Dictionary<string, int> DailyReadingMinutes { get; set; } = new();
    
    // 阅读速度（字/分钟）
    public int AverageReadingSpeed { get; set; }
    
    // 最长连续阅读天数
    public int LongestStreak { get; set; }
    public int CurrentStreak { get; set; }
}
