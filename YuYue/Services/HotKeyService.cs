using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace YuYue.Services;

/// <summary>
/// 全局热键服务
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private IntPtr _windowHandle;
    private HwndSource? _source;
    private int _currentId = 1;
    
    public event EventHandler<HotkeyConflictEventArgs>? HotkeyConflict;
    
    /// <summary>
    /// 初始化热键服务
    /// </summary>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);
    }
    
    /// <summary>
    /// 注册全局热键
    /// </summary>
    public bool RegisterHotkey(ModifierKeys modifiers, Key key, Action action, string description = "")
    {
        var id = _currentId++;
        var vk = KeyInterop.VirtualKeyFromKey(key);
        var fsModifiers = GetModifiers(modifiers);
        
        var success = RegisterHotKey(_windowHandle, id, fsModifiers, (uint)vk);
        
        if (success)
        {
            _hotkeyActions[id] = action;
        }
        else
        {
            HotkeyConflict?.Invoke(this, new HotkeyConflictEventArgs
            {
                Modifiers = modifiers,
                Key = key,
                Description = description
            });
        }
        
        return success;
    }
    
    /// <summary>
    /// 注销所有热键
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var id in _hotkeyActions.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }
        _hotkeyActions.Clear();
    }
    
    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }
    
    private static uint GetModifiers(ModifierKeys modifiers)
    {
        uint fsModifiers = 0;
        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            fsModifiers |= 0x0001; // MOD_ALT
        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            fsModifiers |= 0x0002; // MOD_CONTROL
        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            fsModifiers |= 0x0004; // MOD_SHIFT
        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            fsModifiers |= 0x0008; // MOD_WIN
        return fsModifiers;
    }
    
    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(HwndHook);
    }
}

public class HotkeyConflictEventArgs : EventArgs
{
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }
    public string Description { get; set; } = "";
    
    public string DisplayText => $"{Modifiers}+{Key}";
}
