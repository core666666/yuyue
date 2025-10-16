using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
    
    // Windows API for resizing
    private const int WM_SYSCOMMAND = 0x112;
    private const int SC_SIZE = 0xF000;
    private const int WMSZ_LEFT = 1;
    private const int WMSZ_RIGHT = 2;
    private const int WMSZ_TOP = 3;
    private const int WMSZ_TOPLEFT = 4;
    private const int WMSZ_TOPRIGHT = 5;
    private const int WMSZ_BOTTOM = 6;
    private const int WMSZ_BOTTOMLEFT = 7;
    private const int WMSZ_BOTTOMRIGHT = 8;
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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
                // 左侧菜单在无边框模式下始终隐藏，不需要在这里处理
                if (ReaderBottomNav != null)
                {
                    ReaderBottomNav.Visibility = Visibility.Collapsed;
                }
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
            ResizeMode = ResizeMode.CanResizeWithGrip; // 允许调整窗口大小
            Background = System.Windows.Media.Brushes.Transparent;
            Effect = null;
            
            // 隐藏所有UI元素（包括左侧菜单）
            TitleBarBorder.Visibility = Visibility.Collapsed;
            LeftMenuBorder.Visibility = Visibility.Collapsed; // 初始隐藏左侧菜单
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
            
            // ScrollViewer 隐藏滚动条并设置为透明
            ReaderScrollViewer.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden;
            ReaderScrollViewer.Background = System.Windows.Media.Brushes.Transparent;
            
            // TextBlock 也设置为透明，并调整内容高度使一屏完全展示
            ReaderTextBlock.Background = System.Windows.Media.Brushes.Transparent;
            ReaderTextBlock.Height = ActualHeight; // 设置为窗口高度
            ReaderTextBlock.VerticalAlignment = VerticalAlignment.Stretch;
            
            // 隐藏底部导航按钮区域
            if (ReaderBottomNav != null)
            {
                ReaderBottomNav.Visibility = Visibility.Collapsed;
            }
            
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
            
            // 恢复 ScrollViewer 滚动条和背景
            ReaderScrollViewer.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
            ReaderScrollViewer.ClearValue(System.Windows.Controls.ScrollViewer.BackgroundProperty);
            
            // 恢复 TextBlock 背景和高度
            ReaderTextBlock.ClearValue(System.Windows.Controls.TextBlock.BackgroundProperty);
            ReaderTextBlock.ClearValue(System.Windows.Controls.TextBlock.HeightProperty);
            ReaderTextBlock.ClearValue(System.Windows.Controls.TextBlock.VerticalAlignmentProperty);
            
            // 显示底部导航按钮区域
            if (ReaderBottomNav != null)
            {
                ReaderBottomNav.Visibility = Visibility.Visible;
            }
            
            // 隐藏无边框控制面板
            BorderlessControlPanel.Visibility = Visibility.Collapsed;
        }
    }
    
    private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_viewModel.IsBorderless)
        {
            // 获取鼠标位置
            System.Windows.Point position = e.GetPosition(this);
            int resizeDirection = GetResizeDirection(position);
            
            // 更新鼠标光标
            Cursor = GetResizeCursor(resizeDirection);
            
            // 鼠标移动时显示控制面板和底部导航按钮
            BorderlessControlPanel.Visibility = Visibility.Visible;
            // 左侧菜单在无边框模式下始终隐藏，不显示
            if (ReaderBottomNav != null)
            {
                ReaderBottomNav.Visibility = Visibility.Visible;
            }
            
            // 重置隐藏计时器
            _borderlessHideTimer?.Stop();
            _borderlessHideTimer?.Start();
        }
        else
        {
            // 非无边框模式，恢复默认光标
            Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }
    
    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsBorderless)
        {
            // 获取鼠标位置
            System.Windows.Point position = e.GetPosition(this);
            int resizeDirection = GetResizeDirection(position);
            
            // 如果在边缘，开始调整大小
            if (resizeDirection != 0)
            {
                ResizeWindow(resizeDirection);
            }
            else
            {
                // 如果不在边缘，拖动窗口
                try
                {
                    DragMove();
                }
                catch
                {
                    // 忽略拖动异常
                }
            }
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
    
    private void LeftMenuDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 拖动窗口
        if (e.ClickCount == 1)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // 忽略拖动异常
            }
        }
    }
    
    private void ResizeWindow(int direction)
    {
        if (_hwndSource?.Handle == null)
            return;
            
        SendMessage(_hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + direction), IntPtr.Zero);
    }
    
    private int GetResizeDirection(System.Windows.Point position)
    {
        const int resizeBorderThickness = 8;
        
        bool isLeft = position.X <= resizeBorderThickness;
        bool isRight = position.X >= ActualWidth - resizeBorderThickness;
        bool isTop = position.Y <= resizeBorderThickness;
        bool isBottom = position.Y >= ActualHeight - resizeBorderThickness;
        
        if (isTop && isLeft) return WMSZ_TOPLEFT;
        if (isTop && isRight) return WMSZ_TOPRIGHT;
        if (isBottom && isLeft) return WMSZ_BOTTOMLEFT;
        if (isBottom && isRight) return WMSZ_BOTTOMRIGHT;
        if (isTop) return WMSZ_TOP;
        if (isBottom) return WMSZ_BOTTOM;
        if (isLeft) return WMSZ_LEFT;
        if (isRight) return WMSZ_RIGHT;
        
        return 0;
    }
    
    private System.Windows.Input.Cursor GetResizeCursor(int direction)
    {
        return direction switch
        {
            WMSZ_LEFT or WMSZ_RIGHT => System.Windows.Input.Cursors.SizeWE,
            WMSZ_TOP or WMSZ_BOTTOM => System.Windows.Input.Cursors.SizeNS,
            WMSZ_TOPLEFT or WMSZ_BOTTOMRIGHT => System.Windows.Input.Cursors.SizeNWSE,
            WMSZ_TOPRIGHT or WMSZ_BOTTOMLEFT => System.Windows.Input.Cursors.SizeNESW,
            _ => System.Windows.Input.Cursors.Arrow
        };
    }
}
