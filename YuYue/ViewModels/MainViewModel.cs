using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    private string _fullContent = string.Empty;
    private Book? _currentBook;

    private static readonly SolidColorBrush LightReaderBackground = new(MediaColor.FromRgb(0xFB, 0xFC, 0xFE));
    private static readonly SolidColorBrush DarkReaderBackground = new(MediaColor.FromRgb(0x1E, 0x25, 0x33));
    private static readonly SolidColorBrush LightReaderForeground = new(MediaColor.FromRgb(0x2C, 0x3E, 0x50));
    private static readonly SolidColorBrush DarkReaderForeground = new(MediaColor.FromRgb(0xEC, 0xF0, 0xF1));

    private const int DefaultPageSize = 1200;
    private const int MinPageSize = 600;
    private const int MaxPageSize = 5000;
    private const int PageSizeStep = 400;

    private const double DefaultFontSize = 16;
    private const double MinFontSize = 12;
    private const double MaxFontSize = 36;
    private const double FontSizeStep = 1.0;

    public MainViewModel(LibraryService libraryService, TextContentService textContentService)
    {
        _libraryService = libraryService;
        _textContentService = textContentService;
        readerLineHeight = Math.Round(DefaultFontSize * 1.6, 1);
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

    public int CurrentPageNumber => PageSize == 0 ? 0 : CurrentOffset / PageSize;

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalLength / PageSize);

    public string ProgressDisplay => TotalPages <= 0
        ? "0 / 0"
        : $"{Math.Clamp(CurrentPageNumber + 1, 1, TotalPages)} / {TotalPages}";

    public double ProgressPercentage => TotalLength == 0
        ? 0
        : Math.Clamp(Math.Round((double)CurrentOffset / TotalLength * 100, 1), 0, 100);

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

            ActiveSection = MainSection.Reader;
            StatusMessage = $"正在阅读《{book.Title}》。";
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
    }

    [RelayCommand]
    private async Task BackToBookshelfAsync()
    {
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
}
