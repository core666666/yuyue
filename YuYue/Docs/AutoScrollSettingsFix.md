# 自动滚动设置未生效问题修复

## 问题描述
1. 用户在"自动滚动设置"窗口中修改了滚动速度、翻页间隔等参数后，这些设置没有被保存到配置文件中，导致下次启动应用时设置丢失
2. 设置滚动速度为1或50没有任何区别，速度设置不生效
3. 设置滚动速度为1后，保存并重新打开设置窗口，速度变成了10

## 问题原因
1. **设置未保存**：在 `OpenAutoScrollSettings` 方法中，虽然更新了内存中的设置值，但没有调用 `SavePreferencesAsync()` 方法
2. **速度被强制限制**：`OnAutoScrollSpeedChanged` 方法中有 `if (value < 10) AutoScrollSpeed = 10;` 的限制，导致低于10的速度被强制改为10
3. **速度变化未应用**：修改速度后没有重新启动滚动定时器，导致新速度不生效

## 修复方案

### 修改的文件
- `YuYue/ViewModels/MainViewModel.cs` - 主视图模型
- `YuYue/Views/AutoScrollSettingsWindow.xaml` - 设置窗口界面

### 具体修改

#### 1. 添加设置保存逻辑
将 `OpenAutoScrollSettings` 方法改为异步方法，并在设置更新后调用 `SavePreferencesAsync()`：

```csharp
// 修改前
[RelayCommand]
private void OpenAutoScrollSettings()
{
    var window = new Views.AutoScrollSettingsWindow(AutoScrollMode, AutoPageInterval, AutoScrollSpeed)
    {
        Owner = System.Windows.Application.Current.MainWindow
    };
    
    if (window.ShowDialog() == true)
    {
        AutoScrollMode = window.SelectedMode;
        AutoPageInterval = window.Interval;
        AutoScrollSpeed = window.Speed;
        
        // 如果正在自动滚动，重新启动以应用新设置
        if (IsAutoPageEnabled)
        {
            StartAutoScroll();
        }
        
        StatusMessage = "自动滚动设置已更新";
    }
}

// 修改后
[RelayCommand]
private async Task OpenAutoScrollSettingsAsync()
{
    var window = new Views.AutoScrollSettingsWindow(AutoScrollMode, AutoPageInterval, AutoScrollSpeed)
    {
        Owner = System.Windows.Application.Current.MainWindow
    };
    
    if (window.ShowDialog() == true)
    {
        AutoScrollMode = window.SelectedMode;
        AutoPageInterval = window.Interval;
        AutoScrollSpeed = window.Speed;
        
        // 保存设置到文件
        await SavePreferencesAsync();
        
        // 如果正在自动滚动，重新启动以应用新设置
        if (IsAutoPageEnabled)
        {
            StartAutoScroll();
        }
        
        StatusMessage = "自动滚动设置已更新并保存";
    }
}
```

#### 2. 移除速度最小值限制并添加重启逻辑

```csharp
// 修改前
partial void OnAutoScrollSpeedChanged(int value)
{
    if (value < 10) AutoScrollSpeed = 10;  // 强制最小值为10
    if (value > 500) AutoScrollSpeed = 500;
    
    _ = SavePreferencesAsync();
}

// 修改后
partial void OnAutoScrollSpeedChanged(int value)
{
    // 允许更低的速度，最小值为1
    if (value < 1) AutoScrollSpeed = 1;
    if (value > 500) AutoScrollSpeed = 500;
    
    // 如果正在自动滚动，重新启动以应用新速度
    if (IsAutoPageEnabled && AutoScrollMode == AutoScrollMode.SmoothScroll)
    {
        StartAutoScroll();
    }
    
    _ = SavePreferencesAsync();
}
```

#### 3. 优化低速滚动计算

```csharp
// 修改前
var pixelsPerTick = AutoScrollSpeed * 0.016;
if (pixelsPerTick < 1.0)
{
    pixelsPerTick = 1.0;  // 强制最小1像素，导致速度1和速度10没区别
}

// 修改后
var pixelsPerTick = AutoScrollSpeed * 0.016;
if (pixelsPerTick < 0.1)
{
    pixelsPerTick = 0.1;  // 允许更小的滚动量，支持慢速滚动
}
```

#### 4. 更新设置窗口的速度范围

```xml
<!-- 修改前 -->
<Slider x:Name="SliderSpeed"
        Minimum="1" Maximum="50" 
        Value="50"
        TickFrequency="3"/>

<!-- 修改后 -->
<Slider x:Name="SliderSpeed"
        Minimum="1" Maximum="100" 
        Value="50"
        TickFrequency="1"/>
```

#### 5. 更新快速设置按钮

```xml
<!-- 修改前 -->
<Button Content="慢速 (2)" Tag="2"/>
<Button Content="中速 (10)" Tag="10"/>
<Button Content="快速 (30)" Tag="30"/>
<Button Content="极速 (50)" Tag="50"/>

<!-- 修改后 -->
<Button Content="极慢 (1)" Tag="1"/>
<Button Content="慢速 (5)" Tag="5"/>
<Button Content="中速 (20)" Tag="20"/>
<Button Content="快速 (50)" Tag="50"/>
<Button Content="极速 (100)" Tag="100"/>
```

## 工作原理

1. **设置加载**：应用启动时，`MainWindow_Loaded` 调用 `LoadPreferencesAsync()` 从配置文件加载所有设置
2. **设置修改**：用户在设置窗口修改参数后，点击"确定"按钮
3. **立即保存**：修改后立即调用 `SavePreferencesAsync()` 将设置保存到配置文件
4. **应用关闭**：应用关闭时，`MainWindow_Closing` 再次调用 `SavePreferencesAsync()` 确保所有设置被保存

## 配置文件位置
设置保存在：`%LocalAppData%\YuYue\preferences.json`

## 速度说明

- **速度范围**：1-100 px/s（像素/秒）
- **速度1**：极慢速度，适合逐字阅读
- **速度5**：慢速，适合仔细阅读
- **速度20**：中速，适合正常阅读
- **速度50**：快速，适合快速浏览
- **速度100**：极速，适合扫读

滚动实现采用60fps刷新率（每16ms更新一次），即使是速度1（1px/s）也能实现平滑滚动。

## 测试步骤

### 测试1：设置保存
1. 启动应用
2. 打开"自动滚动设置"窗口
3. 修改滚动速度为1
4. 点击"确定"保存
5. 关闭应用
6. 重新启动应用
7. 再次打开"自动滚动设置"窗口
8. ✅ 验证速度仍然是1（不会变成10）

### 测试2：速度生效
1. 打开一本书
2. 选择"平滑滚动"模式
3. 设置速度为1，启动自动滚动
4. 观察滚动速度（应该非常慢）
5. 暂停，修改速度为50
6. 重新启动自动滚动
7. ✅ 验证滚动速度明显变快

### 测试3：低速滚动
1. 设置速度为1
2. 启动自动滚动
3. ✅ 验证内容能够平滑滚动（不会卡住不动）

## 相关文件
- `YuYue/ViewModels/MainViewModel.cs` - 主视图模型
- `YuYue/Services/PreferencesService.cs` - 偏好设置服务
- `YuYue/Models/ReadingPreferences.cs` - 偏好设置模型
- `YuYue/Views/AutoScrollSettingsWindow.xaml` - 设置窗口界面
- `YuYue/Views/AutoScrollSettingsWindow.xaml.cs` - 设置窗口代码
