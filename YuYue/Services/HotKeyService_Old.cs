using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using YuYue.Services.Interop;

namespace YuYue.Services;

/// <summary>
/// 旧版热键服务（向后兼容）
/// </summary>
public class HotKeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotKeyActions = new();
    private IntPtr _windowHandle;
    private int _nextId = 1;

    public void Initialize(IntPtr handle)
    {
        _windowHandle = handle;
    }

    public int RegisterHotKey(string hotKeyString, Action action)
    {
        var (modifiers, key) = ParseHotKeyString(hotKeyString);
        var id = _nextId++;
        
        var vk = KeyInterop.VirtualKeyFromKey(key);
        var success = NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, (uint)vk);
        
        if (!success)
        {
            throw new InvalidOperationException($"无法注册热键: {hotKeyString}");
        }
        
        _hotKeyActions[id] = action;
        return id;
    }

    public void ProcessHotKeyMessage(int id)
    {
        if (_hotKeyActions.TryGetValue(id, out var action))
        {
            action?.Invoke();
        }
    }

    private static (uint modifiers, Key key) ParseHotKeyString(string hotKeyString)
    {
        uint modifiers = 0;
        var parts = hotKeyString.Split('+');
        var keyString = parts[^1];
        
        foreach (var part in parts[..^1])
        {
            modifiers |= (uint)(part.Trim().ToLower() switch
            {
                "ctrl" or "control" => 0x0002, // MOD_CONTROL
                "alt" => 0x0001, // MOD_ALT
                "shift" => 0x0004, // MOD_SHIFT
                "win" or "windows" => 0x0008, // MOD_WIN
                _ => 0
            });
        }
        
        var key = Enum.TryParse<Key>(keyString.Trim(), true, out var parsedKey) 
            ? parsedKey 
            : Key.None;
        
        return (modifiers, key);
    }

    public void Dispose()
    {
        foreach (var id in _hotKeyActions.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }
        _hotKeyActions.Clear();
    }
}
