using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using YuYue.Models;
using YuYue.ViewModels;

namespace YuYue.Views;

public partial class CamouflageWindow : Window
{
    private readonly MainViewModel _viewModel;

    public CamouflageWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        
        // 为 Excel 伪装提供示例数据
        ExcelDataGrid.ItemsSource = GenerateSampleExcelData();
        
        // 窗口关闭时通知 ViewModel
        Closed += (s, e) => viewModel.IsCamouflageMode = false;
        
        // 处理按键事件
        PreviewKeyDown += CamouflageWindow_PreviewKeyDown;
    }

    private void CamouflageWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // F12 或 Esc 关闭伪装窗口
        if (e.Key == Key.F12 || e.Key == Key.Escape)
        {
            e.Handled = true;
            _viewModel.IsCamouflageMode = false;
        }
        // Ctrl+Tab 切换伪装类型
        else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            if (_viewModel.NextCamouflageCommand.CanExecute(null))
            {
                _viewModel.NextCamouflageCommand.Execute(null);
            }
        }
    }

    private List<ExcelData> GenerateSampleExcelData()
    {
        return new List<ExcelData>
        {
            new() { Date = "2024-01-01", Product = "产品A", Sales = "¥125,000", Cost = "¥80,000", Profit = "¥45,000", Growth = "+12.5%" },
            new() { Date = "2024-01-02", Product = "产品B", Sales = "¥98,500", Cost = "¥65,000", Profit = "¥33,500", Growth = "+8.3%" },
            new() { Date = "2024-01-03", Product = "产品C", Sales = "¥156,000", Cost = "¥95,000", Profit = "¥61,000", Growth = "+15.2%" },
            new() { Date = "2024-01-04", Product = "产品A", Sales = "¥132,000", Cost = "¥82,000", Profit = "¥50,000", Growth = "+5.6%" },
            new() { Date = "2024-01-05", Product = "产品D", Sales = "¥89,000", Cost = "¥58,000", Profit = "¥31,000", Growth = "-3.2%" },
            new() { Date = "2024-01-06", Product = "产品B", Sales = "¥105,000", Cost = "¥68,000", Profit = "¥37,000", Growth = "+6.6%" },
            new() { Date = "2024-01-07", Product = "产品C", Sales = "¥168,000", Cost = "¥98,000", Profit = "¥70,000", Growth = "+7.7%" },
            new() { Date = "2024-01-08", Product = "产品A", Sales = "¥142,000", Cost = "¥85,000", Profit = "¥57,000", Growth = "+7.6%" },
            new() { Date = "2024-01-09", Product = "产品E", Sales = "¥78,500", Cost = "¥52,000", Profit = "¥26,500", Growth = "+4.1%" },
            new() { Date = "2024-01-10", Product = "产品B", Sales = "¥112,000", Cost = "¥70,000", Profit = "¥42,000", Growth = "+6.7%" },
            new() { Date = "2024-01-11", Product = "产品C", Sales = "¥175,000", Cost = "¥102,000", Profit = "¥73,000", Growth = "+4.2%" },
            new() { Date = "2024-01-12", Product = "产品D", Sales = "¥95,000", Cost = "¥60,000", Profit = "¥35,000", Growth = "+6.7%" },
            new() { Date = "2024-01-13", Product = "产品A", Sales = "¥138,000", Cost = "¥84,000", Profit = "¥54,000", Growth = "-2.8%" },
            new() { Date = "2024-01-14", Product = "产品E", Sales = "¥82,000", Cost = "¥54,000", Profit = "¥28,000", Growth = "+4.5%" },
            new() { Date = "2024-01-15", Product = "产品B", Sales = "¥118,000", Cost = "¥72,000", Profit = "¥46,000", Growth = "+5.4%" }
        };
    }
}

