using System;
using System.Collections.Generic;
using YuYue.Services.Interop;

namespace YuYue.Services;

/// <summary>
/// Provides registration and dispatching of system-wide hotkeys.
/// </summary>
public sealed class HotKeyService : IDisposable
{
    private const uint ModifierAlt = 0x0001;
    private const uint ModifierControl = 0x0002;
    private const uint ModifierShift = 0x0004;
    private const uint ModifierWin = 0x0008;

    private readonly Dictionary<int, Action> _callbacks = new();
    private IntPtr _windowHandle;
    private int _currentId;
    private bool _isInitialized;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _isInitialized = true;
    }

    public int RegisterHotKey(string gesture, Action callback)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("HotKeyService 尚未初始化。");
        }

        if (!TryParseGesture(gesture, out var modifiers, out var virtualKey))
        {
            throw new ArgumentException("Gesture 格式应为例如 \"Ctrl+Shift+F\"。", nameof(gesture));
        }

        var id = ++_currentId;
        if (!NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
        {
            throw new InvalidOperationException($"注册热键 {gesture} 失败，可能与系统其他热键冲突。");
        }

        _callbacks[id] = callback;
        return id;
    }

    public void ProcessHotKeyMessage(int hotKeyId)
    {
        if (_callbacks.TryGetValue(hotKeyId, out var action))
        {
            action.Invoke();
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }

        _callbacks.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
    }

    private static bool TryParseGesture(string gesture, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var segment = parts[i];
            if (i == parts.Length - 1 && segment.Length == 1)
            {
                virtualKey = segment.ToUpperInvariant()[0];
            }
            else
            {
                modifiers |= segment.ToLowerInvariant() switch
                {
                    "alt" => ModifierAlt,
                    "ctrl" or "control" => ModifierControl,
                    "shift" => ModifierShift,
                    "win" or "meta" => ModifierWin,
                    _ => 0
                };
            }
        }

        return virtualKey != 0;
    }
}
