using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using YuYue.Models;
using YuYue.Services;

namespace YuYue.Examples;

/// <summary>
/// 热键与自启动功能演示
/// </summary>
public class HotkeyAndAutoStartDemo
{
    /// <summary>
    /// 演示热键服务的基本使用
    /// </summary>
    public static void DemoHotkeyService()
    {
        Console.WriteLine("=== 热键服务演示 ===\n");
        
        // 1. 创建热键服务实例
        var hotkeyService = new HotkeyService();
        
        // 注意：需要在窗口初始化后调用
        // hotkeyService.Initialize(window);
        
        // 2. 注册全局热键
        Console.WriteLine("注册热键示例：");
        Console.WriteLine("- Ctrl+Alt+Y: 显示/隐藏窗口");
        Console.WriteLine("- Ctrl+Shift+C: 切换伪装模式");
        Console.WriteLine("- F11: 沉浸模式");
        
        // 示例代码（实际使用时需要在窗口加载后）
        /*
        hotkeyService.RegisterHotkey(
            ModifierKeys.Control | ModifierKeys.Alt, 
            Key.Y, 
            () => Console.WriteLine("显示/隐藏窗口"),
            "显示/隐藏窗口"
        );
        
        hotkeyService.RegisterHotkey(
            ModifierKeys.Control | ModifierKeys.Shift, 
            Key.C, 
            () => Console.WriteLine("切换伪装模式"),
            "伪装模式"
        );
        */
        
        // 3. 热键冲突处理
        Console.WriteLine("\n热键冲突检测：");
        Console.WriteLine("当热键已被系统或其他程序占用时，会触发 HotkeyConflict 事件");
        
        /*
        hotkeyService.HotkeyConflict += (sender, e) =>
        {
            Console.WriteLine($"热键冲突：{e.DisplayText} - {e.Description}");
            // 可以提示用户选择其他组合键
        };
        */
        
        // 4. 注销热键
        Console.WriteLine("\n注销所有热键：");
        Console.WriteLine("hotkeyService.UnregisterAll();");
    }
    
    /// <summary>
    /// 演示自启动服务的使用
    /// </summary>
    public static void DemoAutoStartService()
    {
        Console.WriteLine("\n=== 自启动服务演示 ===\n");
        
        var autoStartService = new AutoStartService();
        
        // 1. 检查当前状态
        var isEnabled = autoStartService.IsAutoStartEnabled();
        Console.WriteLine($"当前自启动状态: {(isEnabled ? "已启用" : "未启用")}");
        
        // 2. 启用自启动
        Console.WriteLine("\n启用开机自启动：");
        var success = autoStartService.EnableAutoStart();
        Console.WriteLine($"操作结果: {(success ? "成功" : "失败")}");
        
        // 3. 禁用自启动
        Console.WriteLine("\n禁用开机自启动：");
        success = autoStartService.DisableAutoStart();
        Console.WriteLine($"操作结果: {(success ? "成功" : "失败")}");
        
        // 4. 切换状态
        Console.WriteLine("\n切换自启动状态：");
        success = autoStartService.ToggleAutoStart();
        Console.WriteLine($"操作结果: {(success ? "成功" : "失败")}");
        Console.WriteLine($"新状态: {(autoStartService.IsAutoStartEnabled() ? "已启用" : "未启用")}");
    }
    
    /// <summary>
    /// 演示热键配置的使用
    /// </summary>
    public static void DemoHotkeyConfig()
    {
        Console.WriteLine("\n=== 热键配置演示 ===\n");
        
        // 1. 获取默认配置
        var configs = HotkeyConfig.GetDefaultConfigs();
        Console.WriteLine("默认热键配置：");
        foreach (var config in configs)
        {
            Console.WriteLine($"- {config.Description}: {config.DisplayText} (启用: {config.IsEnabled})");
        }
        
        // 2. 自定义配置
        Console.WriteLine("\n自定义热键配置：");
        var customConfig = new HotkeyConfig
        {
            Action = "CustomAction",
            Description = "自定义操作",
            Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
            Key = Key.X,
            IsEnabled = true
        };
        Console.WriteLine($"新配置: {customConfig.Description} - {customConfig.DisplayText}");
        
        // 3. 保存和加载配置
        Console.WriteLine("\n配置持久化：");
        Console.WriteLine("配置会自动保存到 ReadingPreferences.HotkeyConfigs");
        Console.WriteLine("应用启动时会自动加载并注册热键");
    }
    
    /// <summary>
    /// 演示热键设置窗口的使用
    /// </summary>
    public static void DemoHotkeySettingsWindow()
    {
        Console.WriteLine("\n=== 热键设置窗口演示 ===\n");
        
        Console.WriteLine("使用方法：");
        Console.WriteLine("1. 在设置界面点击'自定义快捷键'按钮");
        Console.WriteLine("2. 在弹出的窗口中查看所有热键配置");
        Console.WriteLine("3. 点击'修改'按钮更改热键");
        Console.WriteLine("4. 在输入对话框中按下新的组合键");
        Console.WriteLine("5. 系统会自动检测冲突");
        Console.WriteLine("6. 点击'保存'应用更改");
        
        Console.WriteLine("\n代码示例：");
        Console.WriteLine(@"
var window = new HotkeySettingsWindow(viewModel.HotkeyConfigs);
if (window.ShowDialog() == true && window.IsSaved)
{
    viewModel.HotkeyConfigs = window.HotkeyConfigs;
    viewModel.RegisterAllHotkeys();
}
        ");
    }
    
    /// <summary>
    /// 完整使用流程演示
    /// </summary>
    public static async Task DemoCompleteWorkflow()
    {
        Console.WriteLine("\n=== 完整使用流程演示 ===\n");
        
        // 1. 应用启动
        Console.WriteLine("1. 应用启动时：");
        Console.WriteLine("   - 加载偏好设置（包括热键配置）");
        Console.WriteLine("   - 初始化热键服务");
        Console.WriteLine("   - 注册所有启用的热键");
        Console.WriteLine("   - 检查自启动状态");
        
        // 2. 用户操作
        Console.WriteLine("\n2. 用户操作：");
        Console.WriteLine("   - 按 Ctrl+Alt+Y 快速显示/隐藏窗口");
        Console.WriteLine("   - 按 Ctrl+Shift+C 切换伪装模式");
        Console.WriteLine("   - 按 F11 进入沉浸模式");
        Console.WriteLine("   - 在设置中自定义热键");
        Console.WriteLine("   - 启用/禁用开机自启动");
        
        // 3. 热键冲突处理
        Console.WriteLine("\n3. 热键冲突处理：");
        Console.WriteLine("   - 系统检测到热键已被占用");
        Console.WriteLine("   - 显示警告提示");
        Console.WriteLine("   - 用户选择其他组合键");
        
        // 4. 配置保存
        Console.WriteLine("\n4. 配置保存：");
        Console.WriteLine("   - 用户修改热键配置");
        Console.WriteLine("   - 点击保存按钮");
        Console.WriteLine("   - 配置写入 preferences.json");
        Console.WriteLine("   - 重新注册热键");
        
        // 5. 应用关闭
        Console.WriteLine("\n5. 应用关闭时：");
        Console.WriteLine("   - 保存所有配置");
        Console.WriteLine("   - 注销所有热键");
        Console.WriteLine("   - 清理资源");
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 最佳实践建议
    /// </summary>
    public static void ShowBestPractices()
    {
        Console.WriteLine("\n=== 最佳实践建议 ===\n");
        
        Console.WriteLine("1. 热键选择：");
        Console.WriteLine("   - 避免使用系统保留的组合键（如 Ctrl+Alt+Del）");
        Console.WriteLine("   - 优先使用 Ctrl+Alt 或 Ctrl+Shift 组合");
        Console.WriteLine("   - 为常用功能分配简单易记的热键");
        
        Console.WriteLine("\n2. 冲突处理：");
        Console.WriteLine("   - 提供清晰的冲突提示");
        Console.WriteLine("   - 允许用户轻松更改热键");
        Console.WriteLine("   - 提供恢复默认配置的选项");
        
        Console.WriteLine("\n3. 用户体验：");
        Console.WriteLine("   - 在界面上显示当前热键");
        Console.WriteLine("   - 提供热键帮助文档");
        Console.WriteLine("   - 支持禁用不需要的热键");
        
        Console.WriteLine("\n4. 自启动：");
        Console.WriteLine("   - 明确告知用户自启动的作用");
        Console.WriteLine("   - 提供简单的开关控制");
        Console.WriteLine("   - 尊重用户的选择");
        
        Console.WriteLine("\n5. 性能优化：");
        Console.WriteLine("   - 只注册启用的热键");
        Console.WriteLine("   - 及时注销不用的热键");
        Console.WriteLine("   - 避免频繁的注册/注销操作");
    }
}
