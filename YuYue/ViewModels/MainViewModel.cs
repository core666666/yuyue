using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YuYue.Models;
using YuYue.Services;

namespace YuYue.ViewModels;

public enum MainSection
{
    Bookshelf,
    Reader,
    Settings
}

/// <summary>
/// Coordinates bookshelf management and reading workflow.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LibraryService _libraryService;
    private readonly TextContentService _textContentService;
    private string _fullContent = string.Empty;
    private Book? _currentBook;

    public MainViewModel(LibraryService libraryService, TextContentService textContentService)
    {
        _libraryService = libraryService;
        _textContentService = textContentService;
    }

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
    private int pageSize = 1200;

    [ObservableProperty]
    private int currentOffset;

    [ObservableProperty]
    private int totalLength;

    partial void OnSelectedBookChanged(Book? value) => OpenSelectedBookCommand.NotifyCanExecuteChanged();

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

    partial void OnPageSizeChanged(int value)
    {
        if (value <= 0)
        {
            PageSize = 800;
            return;
        }

        UpdatePageContent();
        RaiseProgressProperties();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    public int CurrentPageNumber => PageSize == 0 ? 0 : CurrentOffset / PageSize;

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalLength / PageSize);

    public string ProgressDisplay => TotalPages <= 0
        ? "0 / 0"
        : $"{Math.Clamp(CurrentPageNumber + 1, 1, TotalPages)} / {TotalPages}";

    public double ProgressPercentage => TotalLength == 0
        ? 0
        : Math.Clamp(Math.Round((double)CurrentOffset / TotalLength * 100, 1), 0, 100);

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
            StatusMessage = Books.Count == 0
                ? "当前书架为空，导入本地 TXT 文件开始阅读。"
                : $"已加载 {Books.Count} 本书。";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NavigateToBookshelf()
    {
        ActiveSection = MainSection.Bookshelf;
        StatusMessage = "书架";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveSection = MainSection.Settings;
        StatusMessage = "设置（开发中）";
    }

    [RelayCommand]
    private async Task ImportLocalBookAsync()
    {
        var dialog = new OpenFileDialog
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
        if (Books.FirstOrDefault(b => string.Equals(b.FilePath, filePath, StringComparison.OrdinalIgnoreCase)) is { } existing)
        {
            SelectedBook = existing;
            StatusMessage = $"《{existing.Title}》已在书架中。";
            return;
        }

        var title = Path.GetFileNameWithoutExtension(filePath);
        var book = new Book
        {
            Title = string.IsNullOrWhiteSpace(title) ? "未命名小说" : title,
            FilePath = filePath,
            LastOpenedUtc = DateTime.UtcNow
        };

        Books.Add(book);
        SelectedBook = book;
        StatusMessage = $"已导入《{book.Title}》。";

        await PersistLibraryAsync();
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
                PageContent = "（内容为空）";
                return;
            }

            CurrentOffset = Math.Clamp(book.CurrentOffset, 0, TotalLength - 1);
            UpdatePageContent();

            ActiveSection = MainSection.Reader;
            StatusMessage = $"正在阅读《{book.Title}》";
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

        _currentBook.CurrentOffset = CurrentOffset;
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
}
