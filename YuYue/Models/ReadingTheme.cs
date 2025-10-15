namespace YuYue.Models;

public class ReadingTheme
{
    public string Name { get; set; } = string.Empty;
    public System.Windows.Media.Color BackgroundColor { get; set; }
    public System.Windows.Media.Color ForegroundColor { get; set; }
    public bool IsBuiltIn { get; set; }
    
    public System.Windows.Media.SolidColorBrush BackgroundBrush => new(BackgroundColor);
    public System.Windows.Media.SolidColorBrush ForegroundBrush => new(ForegroundColor);
}

public static class ThemePresets
{
    public static ReadingTheme Light => new()
    {
        Name = "日间模式",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xFB, 0xFC, 0xFE),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x2C, 0x3E, 0x50),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Dark => new()
    {
        Name = "夜间模式",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0x1E, 0x25, 0x33),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0xEC, 0xF0, 0xF1),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Sepia => new()
    {
        Name = "羊皮纸",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xF4, 0xE8, 0xD0),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x5C, 0x4B, 0x3A),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Green => new()
    {
        Name = "护眼绿",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xC7, 0xED, 0xCC),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x2C, 0x3E, 0x50),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Blue => new()
    {
        Name = "海洋蓝",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xE3, 0xF2, 0xFD),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x1A, 0x23, 0x7E),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Pink => new()
    {
        Name = "樱花粉",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xFC, 0xE4, 0xEC),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x88, 0x05, 0x50),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Gray => new()
    {
        Name = "灰度",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xE8, 0xE8, 0xE8),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x42, 0x42, 0x42),
        IsBuiltIn = true
    };
    
    public static ReadingTheme Amber => new()
    {
        Name = "琥珀",
        BackgroundColor = System.Windows.Media.Color.FromRgb(0xFF, 0xF8, 0xE1),
        ForegroundColor = System.Windows.Media.Color.FromRgb(0x5D, 0x4E, 0x37),
        IsBuiltIn = true
    };
}
