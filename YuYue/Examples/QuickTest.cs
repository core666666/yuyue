using System;
using System.Threading.Tasks;
using YuYue.Services;
using YuYue.Models;

namespace YuYue.Examples;

/// <summary>
/// 快速测试新功能
/// </summary>
public class QuickTest
{
    public static async Task TestAllNewFeatures()
    {
        Console.WriteLine("=== 鱼阅 v2.0 新功能测试 ===\n");
        
        // 1. 测试自启动服务
        Console.WriteLine("1. 测试开机自启动功能...");
        TestAutoStart();
        
        // 2. 测试热键配置
        Console.WriteLine("\n2. 测试热键配置...");
        TestHotkeyConfig();
        
        // 3. 测试偏好设置服务
        Console.WriteLine("\n3. 测试偏好设置服务...");
        await TestPreferencesService();
        
        Console.WriteLine("\n=== 测试完成 ===");
    }
    
    private static void TestAutoStart()
    {
        var service = new AutoStartService();
        
        // 检查当前状态
        var isEnabled = service.IsAutoStartEnabled();
        Console.WriteLine($"   当前自启动状态: {(isEnabled ? "已启用" : "未启用")}");
        
        // 测试切换
        Console.WriteLine("   测试切换自启动状态...");
        var success = service.ToggleAutoStart();
        Console.WriteLine($"   操作结果: {(success ? "成功" : "失败")}");
        
        // 再次检查
        isEnabled = service.IsAutoStartEnabled();
        Console.WriteLine($"   新状态: {(isEnabled ? "已启用" : "未启用")}");
        
        // 恢复原状态
        service.ToggleAutoStart();
        Console.WriteLine("   已恢复原状态");
    }
    
    private static void TestHotkeyConfig()
    {
        // 获取默认配置
        var configs = HotkeyConfig.GetDefaultConfigs();
        Console.WriteLine($"   默认热键配置数量: {configs.Count}");
        
        // 显示前3个配置
        Console.WriteLine("   前3个热键配置:");
        for (int i = 0; i < Math.Min(3, configs.Count); i++)
        {
            var config = configs[i];
            Console.WriteLine($"   - {config.Description}: {config.DisplayText} (启用: {config.IsEnabled})");
        }
        
        // 测试自定义配置
        var customConfig = new HotkeyConfig
        {
            Action = "TestAction",
            Description = "测试热键",
            Modifiers = System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt,
            Key = System.Windows.Input.Key.T,
            IsEnabled = true
        };
        Console.WriteLine($"   自定义配置: {customConfig.Description} - {customConfig.DisplayText}");
    }
    
    private static async Task TestPreferencesService()
    {
        var service = new PreferencesService();
        
        // 加载偏好设置
        Console.WriteLine("   加载偏好设置...");
        var prefs = await service.LoadPreferencesAsync();
        Console.WriteLine($"   字体大小: {prefs.FontSize}");
        Console.WriteLine($"   主题: {prefs.SelectedThemeName}");
        Console.WriteLine($"   自启动: {prefs.AutoStartEnabled}");
        Console.WriteLine($"   热键配置数量: {prefs.HotkeyConfigs.Count}");
        
        // 修改并保存
        Console.WriteLine("   修改设置并保存...");
        prefs.FontSize = 18;
        prefs.AutoStartEnabled = true;
        await service.SavePreferencesAsync(prefs);
        Console.WriteLine("   保存成功");
        
        // 重新加载验证
        Console.WriteLine("   重新加载验证...");
        var reloadedPrefs = await service.LoadPreferencesAsync();
        Console.WriteLine($"   字体大小: {reloadedPrefs.FontSize} (应为 18)");
        Console.WriteLine($"   自启动: {reloadedPrefs.AutoStartEnabled} (应为 True)");
        
        // 恢复原设置
        prefs.FontSize = 16;
        prefs.AutoStartEnabled = false;
        await service.SavePreferencesAsync(prefs);
        Console.WriteLine("   已恢复原设置");
    }
    
    public static void PrintFeatureSummary()
    {
        Console.WriteLine("\n=== 新功能总结 ===\n");
        
        Console.WriteLine("✅ 1. 全局热键系统");
        Console.WriteLine("   - 支持在任何界面使用快捷键");
        Console.WriteLine("   - 默认 Ctrl+Alt+Y 显示/隐藏窗口");
        Console.WriteLine("   - 可自定义所有热键");
        
        Console.WriteLine("\n✅ 2. 热键冲突检测");
        Console.WriteLine("   - 自动检测已占用的组合键");
        Console.WriteLine("   - 显示冲突警告提示");
        Console.WriteLine("   - 支持选择其他组合键");
        
        Console.WriteLine("\n✅ 3. 热键配置界面");
        Console.WriteLine("   - 可视化配置所有快捷键");
        Console.WriteLine("   - 支持启用/禁用单个热键");
        Console.WriteLine("   - 一键恢复默认配置");
        
        Console.WriteLine("\n✅ 4. 开机自启动");
        Console.WriteLine("   - 应用随系统启动");
        Console.WriteLine("   - 简单的开关控制");
        Console.WriteLine("   - 自动配置注册表");
        
        Console.WriteLine("\n✅ 5. 配置持久化");
        Console.WriteLine("   - 所有设置自动保存");
        Console.WriteLine("   - 应用启动时自动加载");
        Console.WriteLine("   - 支持导入/导出（开发中）");
        
        Console.WriteLine("\n使用方法：");
        Console.WriteLine("1. 打开应用，进入‘设置’界面");
        Console.WriteLine("2. 勾选‘开机自动启动’");
        Console.WriteLine("3. 点击‘自定义快捷键’配置热键");
        Console.WriteLine("4. 按 Ctrl+Alt+Y 快速显示/隐藏窗口");
        
        Console.WriteLine("\n详细文档：");
        Console.WriteLine("- 热键与自启动使用说明.md");
        Console.WriteLine("- YuYue/Examples/HotkeyAndAutoStartDemo.cs");
    }
}
