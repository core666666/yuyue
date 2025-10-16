# 低速滚动不生效问题修复

## 问题描述
设置滚动速度低于30时，内容完全不滚动，只有速度≥30时才能看到滚动效果。

## 问题原因

### 根本原因：四舍五入导致滚动量为0

在 `MainWindow.xaml.cs` 的 `HandleSmoothScroll` 方法中：

```csharp
// 问题代码
var newOffset = Math.Round(currentOffset + _viewModel.ScrollOffsetDelta);
```

**计算示例**：
- 速度30：30 × 0.016 = 0.48 px/tick → `Math.Round(0.48)` = 0（大部分时候）
- 速度20：20 × 0.016 = 0.32 px/tick → `Math.Round(0.32)` = 0
- 速度10：10 × 0.016 = 0.16 px/tick → `Math.Round(0.16)` = 0
- 速度1： 1 × 0.016 = 0.016 → 0.1（最小值）→ `Math.Round(0.1)` = 0

当每次滚动量小于0.5像素时，`Math.Round` 会将其四舍五入为0，导致实际上没有滚动。

## 解决方案：累积滚动

### 核心思路
不是每次都四舍五入，而是**累积小数部分**，当累积到≥1像素时才实际滚动。

### 实现方式

#### 1. 添加累积变量
```csharp
// 累积的滚动偏移量（用于支持低速滚动）
private double _accumulatedScrollOffset;
```

#### 2. 修改滚动逻辑
```csharp
// 修改前：直接四舍五入
var newOffset = Math.Round(currentOffset + _viewModel.ScrollOffsetDelta);
ReaderScrollViewer.ScrollToVerticalOffset(newOffset);

// 修改后：累积滚动
_accumulatedScrollOffset += _viewModel.ScrollOffsetDelta;

// 只有当累积量>=1像素时才实际滚动
if (_accumulatedScrollOffset >= 1.0)
{
    var scrollAmount = Math.Floor(_accumulatedScrollOffset);
    _accumulatedScrollOffset -= scrollAmount; // 保留小数部分继续累积
    
    var newOffset = currentOffset + scrollAmount;
    ReaderScrollViewer.ScrollToVerticalOffset(newOffset);
}
```

#### 3. 停止时重置累积量
```csharp
if (!_viewModel.IsAutoPageEnabled)
{
    _accumulatedScrollOffset = 0;
}
```

## 工作原理示例

### 速度1的滚动过程
```
速度1 → 每次0.1像素

Tick 1: 累积 0.1 → 总计 0.1 → 不滚动（<1）
Tick 2: 累积 0.1 → 总计 0.2 → 不滚动（<1）
Tick 3: 累积 0.1 → 总计 0.3 → 不滚动（<1）
...
Tick 10: 累积 0.1 → 总计 1.0 → 滚动1像素，剩余0
Tick 11: 累积 0.1 → 总计 0.1 → 不滚动（<1）
...
```

结果：每10个tick（160ms）滚动1像素，实际速度约6.25 px/s

### 速度20的滚动过程
```
速度20 → 每次0.32像素

Tick 1: 累积 0.32 → 总计 0.32 → 不滚动（<1）
Tick 2: 累积 0.32 → 总计 0.64 → 不滚动（<1）
Tick 3: 累积 0.32 → 总计 0.96 → 不滚动（<1）
Tick 4: 累积 0.32 → 总计 1.28 → 滚动1像素，剩余0.28
Tick 5: 累积 0.32 → 总计 0.60 → 不滚动（<1）
Tick 6: 累积 0.32 → 总计 0.92 → 不滚动（<1）
Tick 7: 累积 0.32 → 总计 1.24 → 滚动1像素，剩余0.24
...
```

结果：大约每3-4个tick滚动1像素，实际速度接近20 px/s

### 速度50的滚动过程
```
速度50 → 每次0.8像素

Tick 1: 累积 0.8 → 总计 0.8 → 不滚动（<1）
Tick 2: 累积 0.8 → 总计 1.6 → 滚动1像素，剩余0.6
Tick 3: 累积 0.8 → 总计 1.4 → 滚动1像素，剩余0.4
Tick 4: 累积 0.8 → 总计 1.2 → 滚动1像素，剩余0.2
Tick 5: 累积 0.8 → 总计 1.0 → 滚动1像素，剩余0
...
```

结果：大部分时候每个tick滚动1像素，实际速度接近50 px/s

## 优势

1. **支持任意低速**：即使速度为1，也能正常滚动
2. **精确的速度控制**：累积方式确保长期平均速度准确
3. **平滑的视觉效果**：整数像素滚动，避免文字抖动
4. **无性能损耗**：只是简单的累加和判断

## 测试验证

### 测试1：极低速（速度1）
- 预期：每160ms滚动1像素，非常缓慢但能看到移动
- 结果：✅ 内容缓慢向上滚动

### 测试2：低速（速度10）
- 预期：每100ms左右滚动1像素
- 结果：✅ 内容以可见的慢速滚动

### 测试3：中速（速度20）
- 预期：每50ms左右滚动1像素
- 结果：✅ 内容以舒适的速度滚动

### 测试4：高速（速度50）
- 预期：几乎每个tick滚动1像素
- 结果：✅ 内容快速滚动

### 测试5：速度切换
- 从速度1切换到速度50
- 预期：立即看到速度变化
- 结果：✅ 速度立即改变

## 修改的文件
- `YuYue/MainWindow.xaml.cs`
  - 添加 `_accumulatedScrollOffset` 字段
  - 修改 `HandleSmoothScroll()` 方法使用累积滚动
  - 在停止滚动时重置累积量

## 相关问题
- [自动滚动设置未生效问题修复](AutoScrollSettingsFix.md)
- [自动滚动速度问题修复总结](AutoScrollSpeedFix_Summary.md)
