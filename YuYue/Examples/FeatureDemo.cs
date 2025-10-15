using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YuYue.Models;
using YuYue.Services;
using YuYue.ViewModels;

namespace YuYue.Examples;

/// <summary>
/// 高级阅读功能演示示例
/// </summary>
public class FeatureDemo
{
    /// <summary>
    /// 演示章节识别功能
    /// </summary>
    public static void DemoChapterExtraction()
    {
        var chapterService = new ChapterService();
        
        var sampleContent = @"
第一章 开始的故事

这是第一章的内容...

第二章 冒险继续

这是第二章的内容...

第三章 高潮来临

这是第三章的内容...
";
        
        var chapters = chapterService.ExtractChapters(sampleContent);
        
        Console.WriteLine($"识别到 {chapters.Count} 个章节：");
        foreach (var chapter in chapters)
        {
            Console.WriteLine($"- {chapter.Title} (位置: {chapter.StartOffset}, 长度: {chapter.Length})");
        }
    }
    
    /// <summary>
    /// 演示阅读计时功能
    /// </summary>
    public static async Task DemoReadingTimer()
    {
        var timerService = new ReadingTimerService();
        
        Console.WriteLine("开始阅读计时...");
        timerService.Start();
        
        // 模拟阅读5秒
        await Task.Delay(5000);
        
        Console.WriteLine($"已阅读: {timerService.CurrentSessionTime.TotalSeconds:F1} 秒");
        
        // 暂停
        timerService.Pause();
        Console.WriteLine("暂停计时");
        
        await Task.Delay(2000);
        
        // 继续
        timerService.Resume();
        Console.WriteLine("继续计时");
        
        await Task.Delay(3000);
        
        var totalMinutes = timerService.Stop();
        Console.WriteLine($"总阅读时长: {totalMinutes} 分钟");
    }
    
    /// <summary>
    /// 演示主题切换功能
    /// </summary>
    public static void DemoThemes()
    {
        var themes = new List<ReadingTheme>
        {
            ThemePresets.Light,
            ThemePresets.Dark,
            ThemePresets.Sepia,
            ThemePresets.Green,
            ThemePresets.Blue,
            ThemePresets.Pink,
            ThemePresets.Gray,
            ThemePresets.Amber
        };
        
        Console.WriteLine("可用主题：");
        foreach (var theme in themes)
        {
            Console.WriteLine($"- {theme.Name}");
            Console.WriteLine($"  背景: RGB({theme.BackgroundColor.R}, {theme.BackgroundColor.G}, {theme.BackgroundColor.B})");
            Console.WriteLine($"  文字: RGB({theme.ForegroundColor.R}, {theme.ForegroundColor.G}, {theme.ForegroundColor.B})");
        }
    }
    
    /// <summary>
    /// 演示书签功能
    /// </summary>
    public static void DemoBookmarks()
    {
        var book = new Book
        {
            Title = "示例小说",
            FilePath = "example.txt",
            TotalLength = 100000
        };
        
        // 添加书签
        book.Bookmarks.Add(new Bookmark
        {
            Name = "精彩片段1",
            Offset = 1000,
            Note = "主角第一次战斗"
        });
        
        book.Bookmarks.Add(new Bookmark
        {
            Name = "精彩片段2",
            Offset = 5000,
            Note = "重要转折点"
        });
        
        book.Bookmarks.Add(new Bookmark
        {
            Name = "精彩片段3",
            Offset = 8000,
            Note = "高潮部分"
        });
        
        Console.WriteLine($"《{book.Title}》的书签：");
        foreach (var bookmark in book.Bookmarks)
        {
            var progress = (double)bookmark.Offset / book.TotalLength * 100;
            Console.WriteLine($"- {bookmark.Name} (进度: {progress:F1}%)");
            if (!string.IsNullOrEmpty(bookmark.Note))
            {
                Console.WriteLine($"  备注: {bookmark.Note}");
            }
        }
    }
    
    /// <summary>
    /// 演示完整的阅读流程
    /// </summary>
    public static async Task DemoCompleteReadingFlow()
    {
        Console.WriteLine("=== 完整阅读流程演示 ===\n");
        
        // 1. 创建服务
        var libraryService = new LibraryService();
        var textService = new TextContentService();
        var chapterService = new ChapterService();
        var timerService = new ReadingTimerService();
        var preferencesService = new PreferencesService();
        var hotkeyService = new HotkeyService();
        var autoStartService = new AutoStartService();
        
        // 2. 创建ViewModel
        var viewModel = new MainViewModel(
            libraryService,
            textService,
            chapterService,
            timerService,
            preferencesService,
            hotkeyService,
            autoStartService
        );
        
        Console.WriteLine("1. 初始化应用...");
        await viewModel.InitializeAsync();
        
        // 3. 选择主题
        Console.WriteLine("\n2. 选择护眼主题...");
        viewModel.SelectedTheme = ThemePresets.Green;
        Console.WriteLine($"   当前主题: {viewModel.SelectedTheme.Name}");
        
        // 4. 调整排版
        Console.WriteLine("\n3. 调整排版设置...");
        viewModel.ReaderFontSize = 18;
        viewModel.ReaderLineHeight = 32;
        viewModel.ParagraphSpacing = 10;
        viewModel.EnableFirstLineIndent = true;
        Console.WriteLine($"   字体: {viewModel.ReaderFontSize}px");
        Console.WriteLine($"   行距: {viewModel.ReaderLineHeight}px");
        Console.WriteLine($"   段距: {viewModel.ParagraphSpacing}px");
        
        // 5. 设置多栏排版
        Console.WriteLine("\n4. 设置排版栏数...");
        viewModel.ColumnCount = 2;
        Console.WriteLine($"   当前: {viewModel.ColumnCount}栏排版");
        
        // 6. 开启自动翻页
        Console.WriteLine("\n5. 开启自动翻页...");
        viewModel.AutoPageInterval = 10;
        Console.WriteLine($"   间隔: {viewModel.AutoPageInterval}秒/页");
        
        // 7. 开始计时
        Console.WriteLine("\n6. 开始阅读计时...");
        timerService.Start();
        Console.WriteLine("   计时器已启动");
        
        // 8. 模拟阅读
        Console.WriteLine("\n7. 模拟阅读过程...");
        await Task.Delay(3000);
        Console.WriteLine($"   已阅读: {timerService.CurrentSessionTime.TotalSeconds:F0}秒");
        
        // 9. 添加书签
        Console.WriteLine("\n8. 添加书签...");
        Console.WriteLine("   书签已添加到当前位置");
        
        // 10. 查看统计
        Console.WriteLine("\n9. 查看阅读统计...");
        Console.WriteLine($"   阅读进度: {viewModel.ProgressPercentage:F1}%");
        Console.WriteLine($"   当前页: {viewModel.CurrentPageNumber + 1}/{viewModel.TotalPages}");
        
        // 11. 进入沉浸模式
        Console.WriteLine("\n10. 进入沉浸模式...");
        viewModel.IsImmersiveMode = true;
        Console.WriteLine("    所有UI已隐藏，专注阅读");
        
        Console.WriteLine("\n=== 演示完成 ===");
    }
    
    /// <summary>
    /// 演示多栏排版计算
    /// </summary>
    public static void DemoColumnLayout()
    {
        Console.WriteLine("=== 多栏排版演示 ===\n");
        
        var pageWidth = 1200; // 页面宽度
        var padding = 32; // 内边距
        var columnGap = 24; // 栏间距
        
        for (int columns = 1; columns <= 3; columns++)
        {
            var availableWidth = pageWidth - (padding * 2);
            var totalGap = columnGap * (columns - 1);
            var columnWidth = (availableWidth - totalGap) / columns;
            
            Console.WriteLine($"{columns}栏排版:");
            Console.WriteLine($"  每栏宽度: {columnWidth}px");
            Console.WriteLine($"  栏间距: {columnGap}px");
            Console.WriteLine($"  适合字数: 约{columnWidth / 16 * 30}字/页");
            Console.WriteLine();
        }
    }
}
