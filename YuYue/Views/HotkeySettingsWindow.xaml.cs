using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using YuYue.Models;
using MessageBox = System.Windows.MessageBox;

namespace YuYue.Views;

public partial class HotkeySettingsWindow : Window
{
    private List<HotkeyConfig> _hotkeyConfigs;
    
    public List<HotkeyConfig> HotkeyConfigs => _hotkeyConfigs;
    public bool IsSaved { get; private set; }
    
    public HotkeySettingsWindow(List<HotkeyConfig> configs)
    {
        InitializeComponent();
        _hotkeyConfigs = configs.Select(c => new HotkeyConfig
        {
            Action = c.Action,
            Description = c.Description,
            Modifiers = c.Modifiers,
            Key = c.Key,
            IsEnabled = c.IsEnabled
        }).ToList();
        
        HotkeyList.ItemsSource = _hotkeyConfigs;
    }
    
    private void EditHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not HotkeyConfig config)
            return;
        
        var dialog = new HotkeyInputDialog(config);
        if (dialog.ShowDialog() == true)
        {
            // 检查冲突
            var conflict = _hotkeyConfigs.FirstOrDefault(c => 
                c != config && 
                c.Modifiers == dialog.Modifiers && 
                c.Key == dialog.Key &&
                c.IsEnabled);
            
            if (conflict != null)
            {
                ShowConflictWarning($"该热键已被 '{conflict.Description}' 使用");
                return;
            }
            
            config.Modifiers = dialog.Modifiers;
            config.Key = dialog.Key;
            HotkeyList.ItemsSource = null;
            HotkeyList.ItemsSource = _hotkeyConfigs;
            HideConflictWarning();
        }
    }
    
    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要恢复默认热键配置吗？",
            "确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _hotkeyConfigs = HotkeyConfig.GetDefaultConfigs();
            HotkeyList.ItemsSource = _hotkeyConfigs;
            HideConflictWarning();
        }
    }
    
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = true;
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
    
    private void ShowConflictWarning(string message)
    {
        ConflictMessage.Text = message;
        ConflictWarning.Visibility = Visibility.Visible;
    }
    
    private void HideConflictWarning()
    {
        ConflictWarning.Visibility = Visibility.Collapsed;
    }
}

/// <summary>
/// 热键输入对话框
/// </summary>
public class HotkeyInputDialog : Window
{
    private readonly System.Windows.Controls.TextBlock _displayText;
    private ModifierKeys _modifiers;
    private Key _key = Key.None;
    
    public ModifierKeys Modifiers => _modifiers;
    public Key Key => _key;
    
    public HotkeyInputDialog(HotkeyConfig config)
    {
        Title = "设置热键";
        Width = 400;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.White;
        
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        
        var title = new System.Windows.Controls.TextBlock
        {
            Text = $"为 '{config.Description}' 设置新热键",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 15)
        };
        System.Windows.Controls.Grid.SetRow(title, 0);
        grid.Children.Add(title);
        
        var border = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 240, 241)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 15)
        };
        
        _displayText = new System.Windows.Controls.TextBlock
        {
            Text = "请按下组合键...",
            FontSize = 16,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        border.Child = _displayText;
        System.Windows.Controls.Grid.SetRow(border, 1);
        grid.Children.Add(border);
        
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        
        var okButton = new System.Windows.Controls.Button
        {
            Content = "确定",
            Width = 80,
            Height = 32,
            Margin = new Thickness(0, 0, 10, 0),
            IsEnabled = false
        };
        okButton.Click += (s, e) => { DialogResult = true; Close(); };
        
        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 80,
            Height = 32
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        Content = grid;
        
        PreviewKeyDown += (s, e) =>
        {
            e.Handled = true;
            
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                return;
            }
            
            _modifiers = Keyboard.Modifiers;
            // 使用 SystemKey 来处理 Alt 组合键的情况
            _key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            
            // 过滤修饰键本身
            if (_key == Key.LeftCtrl || _key == Key.RightCtrl ||
                _key == Key.LeftAlt || _key == Key.RightAlt ||
                _key == Key.LeftShift || _key == Key.RightShift ||
                _key == Key.LWin || _key == Key.RWin ||
                _key == Key.System)
            {
                _displayText.Text = GetModifierText(_modifiers);
                okButton.IsEnabled = false;
                return;
            }
            
            // 构建显示文本
            var displayText = "";
            if (_modifiers != ModifierKeys.None)
            {
                displayText = GetModifierText(_modifiers).TrimEnd('+') + "+";
            }
            displayText += _key.ToString();
            
            _displayText.Text = displayText;
            okButton.IsEnabled = true;
        };
    }
    
    private static string GetModifierText(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");
        if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");
        return parts.Count > 0 ? string.Join("+", parts) + "+" : "请按下组合键...";
    }
}
