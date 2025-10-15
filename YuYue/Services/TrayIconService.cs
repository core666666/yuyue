using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace YuYue.Services;

/// <summary>
/// Encapsulates Windows tray icon behaviour for quick hide/show.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private Action? _onShowRequested;
    private Action? _onExitRequested;

    public TrayIconService()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "鱼阅 - 碎片化阅读器",
            Visible = false
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => _onShowRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => _onExitRequested?.Invoke());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => _onShowRequested?.Invoke();
    }

    public void Configure(Action onShowRequested, Action onExitRequested)
    {
        _onShowRequested = onShowRequested;
        _onExitRequested = onExitRequested;
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Hide()
    {
        _notifyIcon.Visible = false;
    }

    public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 2000)
    {
        if (!_notifyIcon.Visible)
        {
            return;
        }

        try
        {
            _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
        }
        catch
        {
            // Balloon tips may throw on unsupported environments; swallow to avoid crashing.
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
