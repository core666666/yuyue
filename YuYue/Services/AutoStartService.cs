using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace YuYue.Services;

/// <summary>
/// 开机自启动服务
/// </summary>
public class AutoStartService
{
    private const string AppName = "YuYue";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
    /// <summary>
    /// 检查是否已设置开机自启动
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            var value = key?.GetValue(AppName) as string;
            var exePath = GetExecutablePath();
            
            return !string.IsNullOrEmpty(value) && 
                   value.Equals(exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"检查自启动状态失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public bool EnableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
            {
                return false;
            }
            
            var exePath = GetExecutablePath();
            key.SetValue(AppName, exePath);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启用自启动失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null)
            {
                return false;
            }
            
            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"禁用自启动失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 切换自启动状态
    /// </summary>
    public bool ToggleAutoStart()
    {
        return IsAutoStartEnabled() ? DisableAutoStart() : EnableAutoStart();
    }
    
    private static string GetExecutablePath()
    {
        // 使用 AppContext.BaseDirectory 而不是 Assembly.Location
        // 以支持单文件发布
        var appPath = AppContext.BaseDirectory;
        var exeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "YuYue") + ".exe";
        return Path.Combine(appPath, exeName);
    }
}
