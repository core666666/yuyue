using System.Windows;
using YuYue.Models;

namespace YuYue.Views;

public partial class AutoScrollSettingsWindow : Window
{
    public AutoScrollMode SelectedMode { get; private set; }
    public int Interval { get; private set; }
    public int Speed { get; private set; }
    
    public AutoScrollSettingsWindow(AutoScrollMode currentMode, int currentInterval, int currentSpeed)
    {
        InitializeComponent();
        
        // 设置当前值
        RadioDirectPage.IsChecked = currentMode == AutoScrollMode.DirectPage;
        RadioSmoothScroll.IsChecked = currentMode == AutoScrollMode.SmoothScroll;
        SliderInterval.Value = currentInterval;
        SliderSpeed.Value = currentSpeed;
        
        SelectedMode = currentMode;
        Interval = currentInterval;
        Speed = currentSpeed;
    }
    
    private void OK_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = RadioDirectPage.IsChecked == true 
            ? AutoScrollMode.DirectPage 
            : AutoScrollMode.SmoothScroll;
        Interval = (int)SliderInterval.Value;
        Speed = (int)SliderSpeed.Value;
        
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void SetSpeed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            if (int.TryParse(tag, out var speed))
            {
                SliderSpeed.Value = speed;
            }
        }
    }
}
