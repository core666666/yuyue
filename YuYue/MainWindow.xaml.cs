using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CommunityToolkit.Mvvm.Input;
using YuYue.Services;
using YuYue.ViewModels;

namespace YuYue;

/// <summary>
/// Main window hosting bookshelf and reader panels.
/// </summary>
public partial class MainWindow : Window
{
    private const int WmHotKey = 0x0312;

    private readonly MainViewModel _viewModel;
    private readonly HotKeyService _hotKeyService = new();
    private readonly TrayIconService _trayIconService = new();

    private HwndSource? _hwndSource;
    private bool _isHiddenToTray;
    private int _globalToggleHotKeyId;
    private int _bossKeyHotKeyId;
    private Effect? _cachedShadowEffect;
    private System.Windows.Media.Brush? _originalBackground;

    public ICommand HideToTrayCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            new LibraryService(), 
            new TextContentService(),
            new ChapterService(),
            new ReadingTimerService());
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        HideToTrayCommand = new RelayCommand(HideWindowToTray);

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwndSource = (HwndSource?)PresentationSource.FromVisual(this);
        if (_hwndSource is null)
        {
            return;
        }

        _originalBackground = Background;
        _cachedShadowEffect = Effect;
        ApplyBorderless();

        _hwndSource.AddHook(WndProc);
        _hotKeyService.Initialize(_hwndSource.Handle);

        try
        {
            _globalToggleHotKeyId = _hotKeyService.RegisterHotKey("Ctrl+Shift+F", ToggleWindowVisibility);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"注册热键失败（Ctrl+Shift+F）：{ex.Message}";
        }

        try
        {
            _bossKeyHotKeyId = _hotKeyService.RegisterHotKey("Alt+H", HideWindowToTray);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"注册老板键失败（Alt+H）：{ex.Message}";
        }

        _trayIconService.Configure(ShowFromTray, () =>
        {
            Dispatcher.Invoke(Close);
        });
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        if (_viewModel.SaveCurrentProgressCommand is IAsyncRelayCommand command)
        {
            await command.ExecuteAsync(null);
        }

        _hotKeyService.Dispose();
        _trayIconService.Dispose();

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsBorderless))
        {
            Dispatcher.Invoke(ApplyBorderless);
        }
    }

    private async void BooksListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedBook is null)
        {
            return;
        }

        if (_viewModel.OpenSelectedBookCommand is IAsyncRelayCommand command && command.CanExecute(null))
        {
            await command.ExecuteAsync(null);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void ApplyBorderless()
    {
        if (_viewModel.IsBorderless)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            if (_originalBackground is not null)
            {
                Background = _originalBackground;
            }

            if (_cachedShadowEffect is not null)
            {
                Effect = _cachedShadowEffect;
            }
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Effect = null;
            Background = System.Windows.SystemColors.WindowBrush;
        }
    }

    private void ToggleWindowVisibility()
    {
        if (_isHiddenToTray || !IsVisible || WindowState == WindowState.Minimized)
        {
            ShowFromTray();
        }
        else
        {
            HideWindowToTray();
        }
    }

    private void HideWindowToTray()
    {
        if (_isHiddenToTray)
        {
            return;
        }

        _trayIconService.Show();
        _trayIconService.ShowBalloonTip("鱼阅已隐藏", "应用仍在运行，点击此处或按 Ctrl+Shift+F 恢复。");

        _isHiddenToTray = true;
        ShowInTaskbar = false;
        Hide();
    }

    private void ShowFromTray()
    {
        if (!_isHiddenToTray)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                Activate();
            }
            return;
        }

        _trayIconService.Hide();
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _isHiddenToTray = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey)
        {
            _hotKeyService.ProcessHotKeyMessage(wParam.ToInt32());
            handled = true;
        }

        return IntPtr.Zero;
    }
}
