using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YuYue.Models;
using YuYue.Services;
using Brush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace YuYue.ViewModels;

public enum MainSection
{
    Bookshelf,
    Reader,
    Settings
}

/// <summary>
/// 中央视图模型：负责书架、阅读器之间的状态与行为协调。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LibraryService _libraryService;
    private readonly TextContentService _textContentService;
    private readonly ChapterService _chapterService;
    private readonly ReadingTimerService _timerService;
    private readonly PreferencesService _preferencesService;
    private readonly HotkeyService _hotkeyService;
    private readonly AutoStartService _autoStartService;

    private string _fullContent = string.Empty;
    private Book? _currentBook;
    private System.Windows.Threading.DispatcherTimer? _autoPageTimer;

    private static readonly SolidColorBrush LightReaderBackground = new(MediaColor.FromRgb(0xFB, 0xFC, 0xFE));
    private static readonly SolidColorBrush DarkReaderBackground = new(MediaColor.FromRgb(0x1E, 0x25, 0x33));
    private static readonly SolidColorBrush LightReaderForeground = new(MediaColor.FromRgb(0x2C, 0x3E, 0x50));
    private static readonly SolidColorBrush DarkReaderForeground = new(MediaColor.FromRgb(0xEC, 0xF0, 0xF1));

    private const double MinWindowOpacity = 0.4;
    private const double MaxWindowOpacity = 1.0;

    private const int DefaultPageSize = 1200;
    private const int MinPageSize = 600;
    private const int MaxPageSize = 5000;
    private const int PageSizeStep = 400;

    private const double DefaultFontSize = 16;
    private const double MinFontSize = 12;
    private const double MaxFontSize = 36;
    private const double FontSizeStep = 1.0;

    public MainViewModel(LibraryService libraryService, TextContentService textContentService, 
        ChapterService chapterService, ReadingTimerService timerService,
        PreferencesService preferencesService, HotkeyService hotkeyService,
        AutoStartService autoStartService)
    {
        _libraryService = libraryService;
        _textContentService = textContentService;
        _chapterService = chapterService;
        _timerService = timerService;
        _preferencesService = preferencesService;
        _hotkeyService = hotkeyService;
        _autoStartService = autoStartService;
        readerLineHeight = Math.Round(DefaultFontSize * 1.6, 1);
        InitializeCamouflageTemplates();
        InitializeThemes();
    }

    #region 公共属性

    [ObservableProperty]
    private ObservableCollection<Book> books = new();

    [ObservableProperty]
    private Book? selectedBook;

    [ObservableProperty]
    private MainSection activeSection = MainSection.Bookshelf;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string pageContent = string.Empty;

    [ObservableProperty]
    private int pageSize = DefaultPageSize;

    [ObservableProperty]
    private int currentOffset;

    [ObservableProperty]
    private int totalLength;

    [ObservableProperty]
    private bool autoResumeLastBook = true;

    [ObservableProperty]
    private double readerFontSize = DefaultFontSize;

    [ObservableProperty]
    private double readerLineHeight;

    [ObservableProperty]
    private bool useDarkTheme;

    public Brush ReaderBackground => UseDarkTheme ? DarkReaderBackground : LightReaderBackground;

    public Brush ReaderForeground => UseDarkTheme ? DarkReaderForeground : LightReaderForeground;

    [ObservableProperty]
    private bool isCamouflageMode;

    [ObservableProperty]
    private ObservableCollection<CamouflageTemplate> camouflageTemplates = new();

    [ObservableProperty]
    private CamouflageTemplate? selectedCamouflage;

    [ObservableProperty]
    private bool isWindowTopmost;

    [ObservableProperty]
    private double windowOpacity = 0.95;

    [ObservableProperty]
    private bool isBorderless = true;
    
    // 高级阅读功能
    [ObservableProperty]
    private bool isImmersiveMode;
    
    [ObservableProperty]
    private int columnCount = 1;
    
    [ObservableProperty]
    private double paragraphSpacing = 8;
    
    [ObservableProperty]
    private double firstLineIndent = 32;
    
    [ObservableProperty]
    private bool enableFirstLineIndent = true;
    
    [ObservableProperty]
    private ObservableCollection<ReadingTheme> themes = new();
    
    [ObservableProperty]
    private ReadingTheme? selectedTheme;
    
    [ObservableProperty]
    private bool isAutoPageEnabled;
    
    [ObservableProperty]
    private int autoPageInterval = 10;
    
    [ObservableProperty]
    private ObservableCollection<Chapter> chapters = new();
    
    [ObservableProperty]
    private Chapter? selectedChapter;
    
    [ObservableProperty]
    private ObservableCollection<Bookmark> bookmarks = new();
    
    [ObservableProperty]
    private bool isTimerRunning;
    
    [ObservableProperty]
    private string timerDisplay = "00:00:00";
    
    [ObservableProperty]
    private int todayReadingMinutes;
    
    [ObservableProperty]
    private bool showChapterPanel;
    
    [ObservableProperty]
    private bool showBookmarkPanel;
    
    [ObservableProperty]
    private bool showStatisticsPanel;
    
    [ObservableProperty]
    private bool isAutoStartEnabled;
    
    [ObservableProperty]
    private List<HotkeyConfig> hotkeyConfigs = HotkeyConfig.GetDefaultConfigs();

    public string CamouflageStatus => SelectedCamouflage is null
        ? "请选择伪装模板"
        : $"{SelectedCamouflage.DisplayName} · {SelectedCamouflage.Description}";

    public int CurrentPageNumber => PageSize == 0 ? 0 : CurrentOffset / PageSize;

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalLength / PageSize);

    public string ProgressDisplay => TotalPages <= 0
        ? "0 / 0"
        : $"{Math.Clamp(CurrentPageNumber + 1, 1, TotalPages)} / {TotalPages}";

    public double ProgressPercentage => TotalLength == 0
        ? 0
        : Math.Clamp(Math.Round((double)CurrentOffset / TotalLength * 100, 1), 0, 100);
    
    public Brush CurrentBackground => SelectedTheme?.BackgroundBrush ?? ReaderBackground;
    public Brush CurrentForeground => SelectedTheme?.ForegroundBrush ?? ReaderForeground;
    
    // 页面变化事件，用于通知UI滚动到顶部
    public event EventHandler? PageChanged;

    #endregion

    #region 属性变化钩子

    partial void OnSelectedBookChanged(Book? value)
    {
        OpenSelectedBookCommand.NotifyCanExecuteChanged();
        DeleteSelectedBookCommand.NotifyCanExecuteChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        if (value < MinPageSize)
        {
            PageSize = MinPageSize;
            return;
        }

        if (value > MaxPageSize)
        {
            PageSize = MaxPageSize;
            return;
        }

        UpdatePageContent();
        RaiseProgressProperties();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentOffsetChanged(int value)
    {
        RaiseProgressProperties();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnTotalLengthChanged(int value)
    {
        RaiseProgressProperties();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnReaderFontSizeChanged(double value)
    {
        var clamped = Math.Clamp(value, MinFontSize, MaxFontSize);
        if (Math.Abs(clamped - value) > double.Epsilon)
        {
            ReaderFontSize = clamped;
            return;
        }

        ReaderLineHeight = Math.Max(Math.Round(clamped * 1.6, 1), clamped + 6);
    }

    partial void OnReaderLineHeightChanged(double value)
    {
        if (value < ReaderFontSize + 4)
        {
            ReaderLineHeight = ReaderFontSize + 4;
        }
    }

    partial void OnUseDarkThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(ReaderBackground));
        OnPropertyChanged(nameof(ReaderForeground));
    }

    partial void OnWindowOpacityChanged(double value)
    {
        if (value < MinWindowOpacity)
        {
            WindowOpacity = MinWindowOpacity;
        }
        else if (value > MaxWindowOpacity)
        {
            WindowOpacity = MaxWindowOpacity;
        }
    }

    partial void OnIsCamouflageModeChanged(bool value)
    {
        if (value)
        {
            if (SelectedCamouflage is null && CamouflageTemplates.Count > 0)
            {
                SelectedCamouflage = CamouflageTemplates[0];
            }

            if (ActiveSection != MainSection.Reader)
            {
                ActiveSection = MainSection.Reader;
            }
        }

        StatusMessage = value
            ? $"已进入伪装模式（{SelectedCamouflage?.DisplayName ?? "请选择模板"}）。"
            : "已返回阅读模式。";

        OnPropertyChanged(nameof(CamouflageStatus));
    }

    partial void OnSelectedCamouflageChanged(CamouflageTemplate? value)
    {
        OnPropertyChanged(nameof(CamouflageStatus));
        if (value is not null && IsCamouflageMode)
        {
            StatusMessage = $"当前伪装：{value.DisplayName}";
        }
    }

    partial void OnIsWindowTopmostChanged(bool value)
    {
        StatusMessage = value ? "窗口已置顶。" : "窗口已取消置顶。";
    }

    partial void OnIsBorderlessChanged(bool value)
    {
        StatusMessage = value ? "已启用无边框模式。" : "已切换为系统边框。";
    }

    #endregion

    #region 初始化与导航

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            Books = await _libraryService.LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }

        if (Books.Count == 0)
        {
            StatusMessage = "当前书架为空，导入本地 TXT 文件开始阅读。";
            return;
        }

        SortBooksByRecent();
        SelectedBook = Books.First();

        if (AutoResumeLastBook && SelectedBook is not null)
        {
            await OpenBookAsync(SelectedBook);
            StatusMessage = $"自动恢复到《{SelectedBook.Title}》。";
        }
        else
        {
            StatusMessage = $"已加载 {Books.Count} 本书。";
        }
    }

    [RelayCommand]
    private void NavigateToBookshelf()
    {
        ActiveSection = MainSection.Bookshelf;
        StatusMessage = $"书架（共 {Books.Count} 本）";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveSection = MainSection.Settings;
        StatusMessage = "设置（开发中）";
    }

    #endregion

    #region 书架操作

    [RelayCommand]
    private async Task ImportLocalBookAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Multiselect = false,
            Title = "选择要导入的小说文件"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var filePath = dialog.FileName;
        if (!File.Exists(filePath))
        {
            StatusMessage = "文件不存在或无法访问。";
            return;
        }

        var existing = Books.FirstOrDefault(b =>
            string.Equals(b.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedBook = existing;
            StatusMessage = $"《{existing.Title}》已在书架中，直接打开。";
            await OpenBookAsync(existing);
            return;
        }

        var title = Path.GetFileNameWithoutExtension(filePath);
        var book = new Book
        {
            Title = string.IsNullOrWhiteSpace(title) ? "未命名小说" : title.Trim(),
            FilePath = filePath,
            LastOpenedUtc = DateTime.UtcNow
        };

        Books.Add(book);
        SortBooksByRecent();
        SelectedBook = book;

        StatusMessage = $"已导入《{book.Title}》。";

        await PersistLibraryAsync();
        await OpenBookAsync(book);
    }

    [RelayCommand(CanExecute = nameof(CanOpenSelectedBook))]
    private async Task OpenSelectedBookAsync()
    {
        if (SelectedBook is null)
        {
            return;
        }

        await OpenBookAsync(SelectedBook);
    }

    private bool CanOpenSelectedBook() => SelectedBook is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedBook))]
    private async Task DeleteSelectedBookAsync()
    {
        if (SelectedBook is null)
        {
            return;
        }

        var removed = SelectedBook;
        Books.Remove(removed);

        if (_currentBook?.Id == removed.Id)
        {
            ClearReaderState();
        }

        SelectedBook = Books.FirstOrDefault();

        StatusMessage = $"已将《{removed.Title}》从书架移除。";
        await PersistLibraryAsync();
    }

    private bool CanDeleteSelectedBook() => SelectedBook is not null;

    private void SortBooksByRecent()
    {
        if (Books.Count <= 1)
        {
            return;
        }

        var ordered = Books
            .OrderByDescending(b => b.LastOpenedUtc)
            .ThenBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!Books.SequenceEqual(ordered))
        {
            Books = new ObservableCollection<Book>(ordered);
        }
    }

    #endregion

    #region 阅读控制

    public async Task OpenBookAsync(Book book)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _fullContent = await _textContentService.LoadContentAsync(book.FilePath);
            _currentBook = book;

            TotalLength = _fullContent.Length;

            if (TotalLength == 0)
            {
                CurrentOffset = 0;
                PageContent = "（内容为空）";
            }
            else
            {
                CurrentOffset = Math.Clamp(book.CurrentOffset, 0, Math.Max(TotalLength - 1, 0));
                UpdatePageContent();
            }
            
            // 提取章节
            if (book.Chapters.Count == 0)
            {
                book.Chapters = _chapterService.ExtractChapters(_fullContent);
            }
            Chapters = new ObservableCollection<Chapter>(book.Chapters);
            
            // 加载书签
            Bookmarks = new ObservableCollection<Bookmark>(book.Bookmarks);
            
            // 启动计时器
            _timerService.Start(book.TotalReadingMinutes);
            IsTimerRunning = true;
            StartTimerDisplay();

            ActiveSection = MainSection.Reader;
            StatusMessage = $"正在阅读《{book.Title}》（共{Chapters.Count}章）";
            await SaveCurrentProgressAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage()
    {
        if (_fullContent.Length == 0)
        {
            return;
        }

        var newOffset = Math.Max(CurrentOffset - PageSize, 0);
        if (newOffset == CurrentOffset)
        {
            return;
        }

        CurrentOffset = newOffset;
        UpdatePageContent();
        PageChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanGoToPreviousPage() => CurrentOffset > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (_fullContent.Length == 0)
        {
            return;
        }

        var newOffset = Math.Min(CurrentOffset + PageSize, Math.Max(TotalLength - 1, 0));
        if (newOffset == CurrentOffset)
        {
            return;
        }

        CurrentOffset = newOffset;
        UpdatePageContent();
        PageChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanGoToNextPage() => TotalLength > 0 && CurrentOffset + PageSize < TotalLength;

    [RelayCommand]
    private void JumpToBeginning()
    {
        if (TotalLength == 0)
        {
            return;
        }

        CurrentOffset = 0;
        UpdatePageContent();
        PageChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void JumpToEnd()
    {
        if (TotalLength == 0)
        {
            return;
        }

        CurrentOffset = Math.Max(TotalLength - PageSize, 0);
        UpdatePageContent();
        PageChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task BackToBookshelfAsync()
    {
        // 保存阅读时长
        if (_currentBook != null && IsTimerRunning)
        {
            _currentBook.TotalReadingMinutes = _timerService.Stop();
            IsTimerRunning = false;
        }
        
        await SaveCurrentProgressAsync();
        ActiveSection = MainSection.Bookshelf;
        StatusMessage = "返回书架";
    }

    [RelayCommand]
    private async Task SaveCurrentProgressAsync()
    {
        if (_currentBook is null)
        {
            return;
        }

        _currentBook.CurrentOffset = Math.Clamp(CurrentOffset, 0, Math.Max(TotalLength, 0));
        _currentBook.TotalLength = TotalLength;
        _currentBook.LastOpenedUtc = DateTime.UtcNow;

        await PersistLibraryAsync();
    }

    private void UpdatePageContent()
    {
        if (_fullContent.Length == 0)
        {
            PageContent = string.Empty;
            return;
        }

        var length = Math.Min(PageSize, Math.Max(TotalLength - CurrentOffset, 0));
        if (length <= 0)
        {
            length = Math.Min(PageSize, TotalLength);
            CurrentOffset = Math.Max(TotalLength - length, 0);
        }

        PageContent = _fullContent.Substring(CurrentOffset, length);

        if (_currentBook is not null)
        {
            _currentBook.CurrentOffset = CurrentOffset;
            _currentBook.TotalLength = TotalLength;
            _currentBook.LastOpenedUtc = DateTime.UtcNow;
        }

        RaiseProgressProperties();
    }

    private void ClearReaderState()
    {
        _currentBook = null;
        _fullContent = string.Empty;
        PageContent = string.Empty;
        CurrentOffset = 0;
        TotalLength = 0;
        RaiseProgressProperties();
    }

    #endregion

    #region 阅读偏好命令

    [RelayCommand]
    private void IncreaseFontSize()
    {
        ReaderFontSize = Math.Min(ReaderFontSize + FontSizeStep, MaxFontSize);
    }

    [RelayCommand]
    private void DecreaseFontSize()
    {
        ReaderFontSize = Math.Max(ReaderFontSize - FontSizeStep, MinFontSize);
    }

    [RelayCommand]
    private void ResetReaderPreferences()
    {
        ReaderFontSize = DefaultFontSize;
        ReaderLineHeight = Math.Round(DefaultFontSize * 1.6, 1);
        PageSize = DefaultPageSize;
        UseDarkTheme = false;
        StatusMessage = "阅读参数已重置。";
    }

    [RelayCommand]
    private void IncreasePageSize()
    {
        PageSize = Math.Min(PageSize + PageSizeStep, MaxPageSize);
    }

    [RelayCommand]
    private void DecreasePageSize()
    {
        PageSize = Math.Max(PageSize - PageSizeStep, MinPageSize);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        UseDarkTheme = !UseDarkTheme;
        StatusMessage = UseDarkTheme ? "已开启夜间模式。" : "已切换回日间模式。";
    }

    #endregion

    #region 伪装模式

    [RelayCommand]
    private void ToggleCamouflageMode()
    {
        var enable = !IsCamouflageMode;
        IsCamouflageMode = enable;

        if (enable && ActiveSection != MainSection.Reader)
        {
            ActiveSection = MainSection.Reader;
        }
    }

    [RelayCommand]
    private void ExitCamouflageMode()
    {
        IsCamouflageMode = false;
    }

    [RelayCommand]
    private void NextCamouflage()
    {
        if (CamouflageTemplates.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedCamouflage is null
            ? -1
            : CamouflageTemplates.IndexOf(SelectedCamouflage);
        var nextIndex = (currentIndex + 1) % CamouflageTemplates.Count;
        SelectedCamouflage = CamouflageTemplates[nextIndex];
        IsCamouflageMode = true;
        if (ActiveSection != MainSection.Reader)
        {
            ActiveSection = MainSection.Reader;
        }
    }

    #endregion

    #region 持久化支持

    private async Task PersistLibraryAsync()
    {
        try
        {
            await _libraryService.SaveAsync(Books);
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存书架失败：{ex.Message}";
        }
    }

    private void RaiseProgressProperties()
    {
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(ProgressDisplay));
        OnPropertyChanged(nameof(ProgressPercentage));
    }

    #endregion

    private void InitializeCamouflageTemplates()
    {
        var templates = new[]
        {
            new CamouflageTemplate("code", "代码编辑器", "模拟在编辑器中编写代码"),
            new CamouflageTemplate("excel", "数据表格", "展示业务报表与数字"),
            new CamouflageTemplate("browser", "浏览器", "浏览工作相关网页")
        };

        CamouflageTemplates = new ObservableCollection<CamouflageTemplate>(templates);
        SelectedCamouflage = CamouflageTemplates.FirstOrDefault();
        OnPropertyChanged(nameof(CamouflageStatus));
    }
    
    private void InitializeThemes()
    {
        Themes = new ObservableCollection<ReadingTheme>
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
        SelectedTheme = Themes[0];
    }
    
    #region 高级阅读功能
    
    // 极简/沉浸模式
    [RelayCommand]
    private void ToggleImmersiveMode()
    {
        IsImmersiveMode = !IsImmersiveMode;
        StatusMessage = IsImmersiveMode ? "已进入沉浸模式" : "已退出沉浸模式";
    }
    
    // 多栏排版
    [RelayCommand]
    private void SetColumnCount(string count)
    {
        if (int.TryParse(count, out var c) && c >= 1 && c <= 3)
        {
            ColumnCount = c;
            StatusMessage = $"已切换到{c}栏排版";
        }
    }
    
    // 主题切换
    partial void OnSelectedThemeChanged(ReadingTheme? value)
    {
        OnPropertyChanged(nameof(CurrentBackground));
        OnPropertyChanged(nameof(CurrentForeground));
        if (value != null)
        {
            StatusMessage = $"已切换到{value.Name}主题";
        }
    }
    
    [RelayCommand]
    private void NextTheme()
    {
        if (Themes.Count == 0) return;
        var currentIndex = SelectedTheme == null ? -1 : Themes.IndexOf(SelectedTheme);
        var nextIndex = (currentIndex + 1) % Themes.Count;
        SelectedTheme = Themes[nextIndex];
    }
    
    // 自动翻页
    [RelayCommand]
    private void ToggleAutoPage()
    {
        IsAutoPageEnabled = !IsAutoPageEnabled;
        
        if (IsAutoPageEnabled)
        {
            _autoPageTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(AutoPageInterval)
            };
            _autoPageTimer.Tick += (s, e) => NextPage();
            _autoPageTimer.Start();
            StatusMessage = $"已开启自动翻页（{AutoPageInterval}秒/页）";
        }
        else
        {
            _autoPageTimer?.Stop();
            _autoPageTimer = null;
            StatusMessage = "已关闭自动翻页";
        }
    }
    
    partial void OnAutoPageIntervalChanged(int value)
    {
        if (_autoPageTimer != null && value >= 3 && value <= 60)
        {
            _autoPageTimer.Interval = TimeSpan.FromSeconds(value);
        }
    }
    
    // 章节跳转
    [RelayCommand]
    private void JumpToChapter(Chapter? chapter)
    {
        if (chapter == null || _fullContent.Length == 0) return;
        
        CurrentOffset = Math.Clamp(chapter.StartOffset, 0, Math.Max(TotalLength - 1, 0));
        UpdatePageContent();
        PageChanged?.Invoke(this, EventArgs.Empty);
        StatusMessage = $"已跳转到：{chapter.Title}";
    }
    
    [RelayCommand]
    private void ToggleChapterPanel()
    {
        ShowChapterPanel = !ShowChapterPanel;
        if (ShowChapterPanel)
        {
            ShowBookmarkPanel = false;
            ShowStatisticsPanel = false;
        }
    }
    
    [RelayCommand]
    private void PreviousChapter()
    {
        if (Chapters.Count == 0) return;
        
        var current = Chapters.FirstOrDefault(c => c.StartOffset <= CurrentOffset);
        if (current == null)
        {
            JumpToChapter(Chapters[0]);
            return;
        }
        
        var index = Chapters.IndexOf(current);
        if (index > 0)
        {
            JumpToChapter(Chapters[index - 1]);
        }
    }
    
    [RelayCommand]
    private void NextChapter()
    {
        if (Chapters.Count == 0) return;
        
        var current = Chapters.LastOrDefault(c => c.StartOffset <= CurrentOffset);
        if (current == null)
        {
            JumpToChapter(Chapters[0]);
            return;
        }
        
        var index = Chapters.IndexOf(current);
        if (index < Chapters.Count - 1)
        {
            JumpToChapter(Chapters[index + 1]);
        }
    }
    
    // 书签系统
    [RelayCommand]
    private void AddBookmark()
    {
        if (_currentBook == null) return;
        
        var bookmark = new Bookmark
        {
            Name = $"书签 {_currentBook.Bookmarks.Count + 1}",
            Offset = CurrentOffset,
            CreatedUtc = DateTime.UtcNow
        };
        
        _currentBook.Bookmarks.Add(bookmark);
        Bookmarks = new ObservableCollection<Bookmark>(_currentBook.Bookmarks);
        StatusMessage = $"已添加书签：{bookmark.Name}";
    }
    
    [RelayCommand]
    private void JumpToBookmark(Bookmark? bookmark)
    {
        if (bookmark == null) return;
        
        CurrentOffset = Math.Clamp(bookmark.Offset, 0, Math.Max(TotalLength - 1, 0));
        UpdatePageContent();
        StatusMessage = $"已跳转到书签：{bookmark.Name}";
    }
    
    [RelayCommand]
    private void DeleteBookmark(Bookmark? bookmark)
    {
        if (bookmark == null || _currentBook == null) return;
        
        _currentBook.Bookmarks.Remove(bookmark);
        Bookmarks = new ObservableCollection<Bookmark>(_currentBook.Bookmarks);
        StatusMessage = $"已删除书签：{bookmark.Name}";
    }
    
    [RelayCommand]
    private void ToggleBookmarkPanel()
    {
        ShowBookmarkPanel = !ShowBookmarkPanel;
        if (ShowBookmarkPanel)
        {
            ShowChapterPanel = false;
            ShowStatisticsPanel = false;
        }
    }
    
    // 专注计时
    [RelayCommand]
    private void ToggleTimer()
    {
        if (IsTimerRunning)
        {
            _timerService.Pause();
            IsTimerRunning = false;
            StatusMessage = "已暂停计时";
        }
        else
        {
            var previousMinutes = _currentBook?.TotalReadingMinutes ?? 0;
            _timerService.Start(previousMinutes);
            IsTimerRunning = true;
            StatusMessage = "已开始计时";
            StartTimerDisplay();
        }
    }
    
    private void StartTimerDisplay()
    {
        var displayTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        displayTimer.Tick += (s, e) =>
        {
            if (!IsTimerRunning)
            {
                displayTimer.Stop();
                return;
            }
            var elapsed = _timerService.CurrentSessionTime;
            TimerDisplay = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        };
        displayTimer.Start();
    }
    
    [RelayCommand]
    private void ResetTimer()
    {
        _timerService.Reset();
        IsTimerRunning = false;
        TimerDisplay = "00:00:00";
        StatusMessage = "已重置计时器";
    }
    
    [RelayCommand]
    private void ToggleStatisticsPanel()
    {
        ShowStatisticsPanel = !ShowStatisticsPanel;
        if (ShowStatisticsPanel)
        {
            ShowChapterPanel = false;
            ShowBookmarkPanel = false;
            UpdateStatistics();
        }
    }
    
    private void UpdateStatistics()
    {
        if (_currentBook != null)
        {
            TodayReadingMinutes = _currentBook.TotalReadingMinutes;
        }
    }
    
    #endregion
    
    #region 热键与自启动
    
    /// <summary>
    /// 初始化热键
    /// </summary>
    public void InitializeHotkeys(Window window)
    {
        _hotkeyService.Initialize(window);
        _hotkeyService.HotkeyConflict += OnHotkeyConflict;
        RegisterAllHotkeys();
    }
    
    private void RegisterAllHotkeys()
    {
        _hotkeyService.UnregisterAll();
        
        foreach (var config in HotkeyConfigs.Where(c => c.IsEnabled))
        {
            Action? action = config.Action switch
            {
                "ShowHide" => ToggleWindowVisibility,
                "NextPage" => NextPage,
                "PreviousPage" => PreviousPage,
                "NextChapter" => NextChapter,
                "PreviousChapter" => PreviousChapter,
                "AddBookmark" => AddBookmark,
                "ToggleImmersive" => ToggleImmersiveMode,
                "ToggleCamouflage" => ToggleCamouflageMode,
                "SaveProgress" => async () => await SaveCurrentProgressAsync(),
                "BackToBookshelf" => async () => await BackToBookshelfAsync(),
                _ => null
            };
            
            if (action != null)
            {
                _hotkeyService.RegisterHotkey(config.Modifiers, config.Key, action, config.Description);
            }
        }
    }
    
    private void OnHotkeyConflict(object? sender, HotkeyConflictEventArgs e)
    {
        StatusMessage = $"热键冲突：{e.DisplayText} 已被占用";
    }
    
    private void ToggleWindowVisibility()
    {
        // 由 MainWindow 实现
    }
    
    [RelayCommand]
    private void OpenHotkeySettings()
    {
        var window = new Views.HotkeySettingsWindow(HotkeyConfigs);
        if (window.ShowDialog() == true && window.IsSaved)
        {
            HotkeyConfigs = window.HotkeyConfigs;
            RegisterAllHotkeys();
            StatusMessage = "热键配置已更新";
        }
    }
    
    [RelayCommand]
    private void ToggleAutoStart()
    {
        var success = _autoStartService.ToggleAutoStart();
        if (success)
        {
            IsAutoStartEnabled = _autoStartService.IsAutoStartEnabled();
            StatusMessage = IsAutoStartEnabled ? "已启用开机自启动" : "已禁用开机自启动";
        }
        else
        {
            StatusMessage = "设置开机自启动失败";
        }
    }
    
    /// <summary>
    /// 加载偏好设置
    /// </summary>
    public async Task LoadPreferencesAsync()
    {
        var prefs = await _preferencesService.LoadPreferencesAsync();
        
        // 应用设置
        ReaderFontSize = prefs.FontSize;
        ReaderLineHeight = prefs.LineHeight;
        ParagraphSpacing = prefs.ParagraphSpacing;
        EnableFirstLineIndent = prefs.EnableFirstLineIndent;
        FirstLineIndent = prefs.FirstLineIndent;
        ColumnCount = prefs.ColumnCount;
        PageSize = prefs.PageSize;
        UseDarkTheme = prefs.UseDarkTheme;
        IsAutoPageEnabled = prefs.AutoPageEnabled;
        AutoPageInterval = prefs.AutoPageInterval;
        IsWindowTopmost = prefs.WindowTopmost;
        WindowOpacity = prefs.WindowOpacity;
        IsBorderless = prefs.BorderlessMode;
        AutoResumeLastBook = prefs.AutoResumeLastBook;
        HotkeyConfigs = prefs.HotkeyConfigs;
        IsAutoStartEnabled = _autoStartService.IsAutoStartEnabled();
        
        // 应用主题
        var theme = Themes.FirstOrDefault(t => t.Name == prefs.SelectedThemeName);
        if (theme != null)
        {
            SelectedTheme = theme;
        }
    }
    
    /// <summary>
    /// 保存偏好设置
    /// </summary>
    public async Task SavePreferencesAsync()
    {
        var prefs = new ReadingPreferences
        {
            FontSize = ReaderFontSize,
            LineHeight = ReaderLineHeight,
            ParagraphSpacing = ParagraphSpacing,
            EnableFirstLineIndent = EnableFirstLineIndent,
            FirstLineIndent = FirstLineIndent,
            ColumnCount = ColumnCount,
            PageSize = PageSize,
            SelectedThemeName = SelectedTheme?.Name ?? "日间模式",
            UseDarkTheme = UseDarkTheme,
            AutoPageEnabled = IsAutoPageEnabled,
            AutoPageInterval = AutoPageInterval,
            ImmersiveModeEnabled = IsImmersiveMode,
            WindowTopmost = IsWindowTopmost,
            WindowOpacity = WindowOpacity,
            BorderlessMode = IsBorderless,
            AutoResumeLastBook = AutoResumeLastBook,
            HotkeyConfigs = HotkeyConfigs,
            AutoStartEnabled = IsAutoStartEnabled
        };
        
        await _preferencesService.SavePreferencesAsync(prefs);
    }
    
    #endregion
}
