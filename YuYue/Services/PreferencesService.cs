using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YuYue.Models;

namespace YuYue.Services;

/// <summary>
/// 用户偏好设置服务
/// </summary>
public class PreferencesService
{
    private const string PreferencesFileName = "preferences.json";
    private const string StatisticsFileName = "statistics.json";
    
    private readonly string _preferencesPath;
    private readonly string _statisticsPath;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    
    public PreferencesService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YuYue"
        );
        
        Directory.CreateDirectory(appDataPath);
        
        _preferencesPath = Path.Combine(appDataPath, PreferencesFileName);
        _statisticsPath = Path.Combine(appDataPath, StatisticsFileName);
    }
    
    /// <summary>
    /// 加载用户偏好设置
    /// </summary>
    public async Task<ReadingPreferences> LoadPreferencesAsync()
    {
        try
        {
            if (!File.Exists(_preferencesPath))
            {
                return new ReadingPreferences();
            }
            
            var json = await File.ReadAllTextAsync(_preferencesPath);
            return JsonSerializer.Deserialize<ReadingPreferences>(json, JsonOptions) 
                   ?? new ReadingPreferences();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载偏好设置失败: {ex.Message}");
            return new ReadingPreferences();
        }
    }
    
    /// <summary>
    /// 保存用户偏好设置
    /// </summary>
    public async Task SavePreferencesAsync(ReadingPreferences preferences)
    {
        try
        {
            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            await File.WriteAllTextAsync(_preferencesPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存偏好设置失败: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 加载阅读统计数据
    /// </summary>
    public async Task<ReadingStatistics> LoadStatisticsAsync()
    {
        try
        {
            if (!File.Exists(_statisticsPath))
            {
                return new ReadingStatistics();
            }
            
            var json = await File.ReadAllTextAsync(_statisticsPath);
            return JsonSerializer.Deserialize<ReadingStatistics>(json, JsonOptions) 
                   ?? new ReadingStatistics();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载统计数据失败: {ex.Message}");
            return new ReadingStatistics();
        }
    }
    
    /// <summary>
    /// 保存阅读统计数据
    /// </summary>
    public async Task SaveStatisticsAsync(ReadingStatistics statistics)
    {
        try
        {
            var json = JsonSerializer.Serialize(statistics, JsonOptions);
            await File.WriteAllTextAsync(_statisticsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存统计数据失败: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 更新今日阅读时长
    /// </summary>
    public async Task UpdateTodayReadingMinutesAsync(int minutes)
    {
        var statistics = await LoadStatisticsAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        
        // 检查是否是新的一天
        if (statistics.LastReadingDate.Date != DateTime.Today)
        {
            statistics.TodayReadingMinutes = 0;
            statistics.LastReadingDate = DateTime.Today;
            
            // 更新连续阅读天数
            var daysSinceLastReading = (DateTime.Today - statistics.LastReadingDate.Date).Days;
            if (daysSinceLastReading == 1)
            {
                statistics.CurrentStreak++;
                if (statistics.CurrentStreak > statistics.LongestStreak)
                {
                    statistics.LongestStreak = statistics.CurrentStreak;
                }
            }
            else if (daysSinceLastReading > 1)
            {
                statistics.CurrentStreak = 1;
            }
        }
        
        statistics.TodayReadingMinutes += minutes;
        statistics.TotalReadingMinutes += minutes;
        
        // 记录每日阅读时长
        if (statistics.DailyReadingMinutes.ContainsKey(today))
        {
            statistics.DailyReadingMinutes[today] += minutes;
        }
        else
        {
            statistics.DailyReadingMinutes[today] = minutes;
        }
        
        await SaveStatisticsAsync(statistics);
    }
    
    /// <summary>
    /// 重置偏好设置为默认值
    /// </summary>
    public async Task ResetPreferencesAsync()
    {
        var defaultPreferences = new ReadingPreferences();
        await SavePreferencesAsync(defaultPreferences);
    }
    
    /// <summary>
    /// 导出偏好设置
    /// </summary>
    public async Task<string> ExportPreferencesAsync()
    {
        var preferences = await LoadPreferencesAsync();
        return JsonSerializer.Serialize(preferences, JsonOptions);
    }
    
    /// <summary>
    /// 导入偏好设置
    /// </summary>
    public async Task ImportPreferencesAsync(string json)
    {
        var preferences = JsonSerializer.Deserialize<ReadingPreferences>(json, JsonOptions);
        if (preferences != null)
        {
            await SavePreferencesAsync(preferences);
        }
    }
}
