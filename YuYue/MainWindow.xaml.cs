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
using YuYue.Views;

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
    private readonly HotkeyService _hotkeyService = new();
    private readonly PreferencesService _preferencesService = new();
    private readonly AutoStartService _autoStartService = new();

    private HwndSource? _hwndSource;
    private bool _isHiddenToTray;
    private int _globalToggleHotKeyId;
    private int _bossKeyHotKeyId;
    private Effect? _cachedShadowEffect;
    private System.Windows.Media.Brush? _originalBackground;
    private CamouflageWindow? _camouflageWindow;
    private WindowState _stateBeforeCamouflage;
    private System.Windows.Threading.DispatcherTimer? _borderlessHideTimer;

    public ICommand HideToTrayCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            new LibraryService(), 
            new TextContentService(),
            new ChapterService(),
            new ReadingTimerService(),
            _preferencesService,
            _hotkeyService,
            _autoStartService);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.PageChanged += ViewModel_PageChanged;

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
        
        // 初始化无边框模式的鼠标悬浮计时器
        _borderlessHideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _borderlessHideTimer.Tick += (s, e) =>
        {
            if (_viewModel.IsBorderless)
            {
                BorderlessControlPanel.Visibility = Visibility.Collapsed;
                _borderlessHideTimer.Stop();
            }
        };

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
        // 加载偏好设置
        await _viewModel.LoadPreferencesAsync();
        
        // 初始化热键
        _viewModel.InitializeHotkeys(this);
        
        // 初始化应用
        await _viewModel.InitializeAsync();
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

        // 保存阅读进度
        if (_viewModel.SaveCurrentProgressCommand is IAsyncRelayCommand command)
        {
            await command.ExecuteAsync(null);
        }
        
        // 保存偏好设置
        await _viewModel.SavePreferencesAsync();

        _hotKeyService.Dispose();
        _hotkeyService.Dispose();
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
        else if (e.PropertyName == nameof(MainViewModel.IsCamouflageMode))
        {
            Dispatcher.Invoke(HandleCamouflageModeChanged);
        }
    }
    
    private void ViewModel_PageChanged(object? sender, EventArgs e)
    {
        // 翻页后滚动到顶部
        Dispatcher.Invoke(() =>
        {
            ReaderScrollViewer?.ScrollToTop();
        });
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
            // 无边框模式：背景全透明，只显示文字内容
            // 注意：AllowsTransparency 和 WindowStyle 在 XAML 中已设置，不能在运行时更改
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.Transparent;
            Effect = null;
            
            // 隐藏所有UI元素
            TitleBarBorder.Visibility = Visibility.Collapsed;
            LeftMenuBorder.Visibility = Visibility.Collapsed;
            StatusBarBorder.Visibility = Visibility.Collapsed;
            ReaderTopBar.Visibility = Visibility.Collapsed;
            ReaderSidePanel.Visibility = Visibility.Collapsed;
            
            // 主容器完全透明
            MainBorder.CornerRadius = new CornerRadius(0);
            MainBorder.BorderThickness = new Thickness(0);
            MainBorder.Background = System.Windows.Media.Brushes.Transparent;
            
            // 分隔线也透明
            DividerBorder.Visibility = Visibility.Collapsed;
            
            // 主内容区域透明
            MainContentBorder.Background = System.Windows.Media.Brushes.Transparent;
            MainContentBorder.Padding = new Thickness(0);
            MainContentBorder.Margin = new Thickness(0);
            
            // 阅读内容区域也透明，只显示文字
            // 先清除绑定，再设置透明背景
            System.Windows.Data.BindingOperations.ClearBinding(ReaderContentBorder, System.Windows.Controls.Border.BackgroundProperty);
            ReaderContentBorder.Background = System.Windows.Media.Brushes.Transparent;
            ReaderContentBorder.BorderThickness = new Thickness(0);
            ReaderContentBorder.Margin = new Thickness(0);
            
            // ScrollViewer 和 TextBlock 也设置为透明
            ReaderScrollViewer.Background = System.Windows.Media.Brushes.Transparent;
            ReaderTextBlock.Background = System.Windows.Media.Brushes.Transparent;
            
            // 显示无边框控制面板（初始隐藏，鼠标悬浮显示）
            BorderlessControlPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            // 有边框模式：显示所有UI元素
            ResizeMode = ResizeMode.CanResize;
            
            if (_originalBackground is not null)
            {
                Background = _originalBackground;
            }

            if (_cachedShadowEffect is not null)
            {
                Effect = _cachedShadowEffect;
            }
            
            // 显示所有UI元素
            TitleBarBorder.Visibility = Visibility.Visible;
            LeftMenuBorder.Visibility = Visibility.Visible;
            StatusBarBorder.Visibility = Visibility.Visible;
            ReaderTopBar.Visibility = Visibility.Visible;
            ReaderSidePanel.Visibility = Visibility.Visible;
            
            // 恢复正常样式
            MainBorder.CornerRadius = new CornerRadius(12);
            MainBorder.BorderThickness = new Thickness(1);
            MainBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF5, 0xF7, 0xFA));
            
            // 恢复分隔线
            DividerBorder.Visibility = Visibility.Visible;
            
            // 恢复主内容区域
            MainContentBorder.Background = System.Windows.Media.Brushes.White;
            MainContentBorder.Padding = new Thickness(24);
            MainContentBorder.Margin = new Thickness(0, 0, 12, 12);
            
            // 恢复阅读区域背景（重新绑定到 CurrentBackground）
            var backgroundBinding = new System.Windows.Data.Binding("CurrentBackground");
            ReaderContentBorder.SetBinding(System.Windows.Controls.Border.BackgroundProperty, backgroundBinding);
            ReaderContentBorder.BorderThickness = new Thickness(1);
            ReaderContentBorder.Margin = new Thickness(0, 0, 4, 0);
            
            // 恢复 ScrollViewer 和 TextBlock 背景
            ReaderScrollViewer.ClearValue(System.Windows.Controls.ScrollViewer.BackgroundProperty);
            ReaderTextBlock.ClearValue(System.Windows.Controls.TextBlock.BackgroundProperty);
            
            // 隐藏无边框控制面板
            BorderlessControlPanel.Visibility = Visibility.Collapsed;
        }
    }
    
    private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsBorderless)
        {
            // 鼠标移动时显示控制面板
            BorderlessControlPanel.Visibility = Visibility.Visible;
            
            // 重置隐藏计时器
            _borderlessHideTimer?.Stop();
            _borderlessHideTimer?.Start();
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

    private void HandleCamouflageModeChanged()
    {
        if (_viewModel.IsCamouflageMode)
        {
            // 进入伪装模式：最小化主窗口，显示伪装窗口
            _stateBeforeCamouflage = WindowState;
            WindowState = WindowState.Minimized;
            
            if (_camouflageWindow == null || !_camouflageWindow.IsLoaded)
            {
                _camouflageWindow = new CamouflageWindow(_viewModel);
                _camouflageWindow.Closed += CamouflageWindow_Closed;
            }
            
            _camouflageWindow.Show();
            _camouflageWindow.Activate();
        }
        else
        {
            // 退出伪装模式：关闭伪装窗口，恢复主窗口
            if (_camouflageWindow != null)
            {
                _camouflageWindow.Closed -= CamouflageWindow_Closed;
                _camouflageWindow.Close();
                _camouflageWindow = null;
            }
            
            WindowState = _stateBeforeCamouflage;
            Show();
            Activate();
        }
    }

    private void CamouflageWindow_Closed(object? sender, EventArgs e)
    {
        // 伪装窗口关闭时，恢复主窗口
        _viewModel.IsCamouflageMode = false;
    }
    
    private void ExitBorderless_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsBorderless = false;
    }
}
