# 鱼阅项目结构说明

## 目录结构

```
YuYue/
├── App.xaml                    # 应用程序入口（全局资源和启动配置）
├── App.xaml.cs                 # 应用程序代码
├── MainWindow.xaml             # 主窗口视图
├── MainWindow.xaml.cs          # 主窗口代码
├── AssemblyInfo.cs             # 程序集信息
├── YuYue.csproj                # 项目文件
│
├── Views/                      # 视图层（XAML界面）
│   ├── MainView.xaml           # 主视图（替代默认MainWindow）
│   ├── ReaderView.xaml         # 阅读器视图
│   ├── BookshelfView.xaml      # 书架视图
│   ├── SettingsView.xaml       # 设置视图
│   ├── SearchView.xaml         # 搜索视图
│   └── CamouflageViews/        # 伪装界面
│       ├── CodeEditorView.xaml # 代码编辑器伪装
│       ├── ExcelView.xaml      # Excel表格伪装
│       └── WordView.xaml       # Word文档伪装
│
├── ViewModels/                 # 视图模型层（MVVM模式）
│   ├── MainViewModel.cs        # 主视图模型
│   ├── ReaderViewModel.cs      # 阅读器视图模型
│   ├── BookshelfViewModel.cs   # 书架视图模型
│   ├── SettingsViewModel.cs    # 设置视图模型
│   └── SearchViewModel.cs      # 搜索视图模型
│
├── Models/                     # 数据模型层
│   ├── Book.cs                 # 书籍模型
│   ├── Chapter.cs              # 章节模型
│   ├── ReadingProgress.cs      # 阅读进度模型
│   ├── BookSource.cs           # 书源模型
│   ├── UserSettings.cs         # 用户设置模型
│   └── ReadingStatistics.cs    # 阅读统计模型
│
├── Services/                   # 业务逻辑服务层
│   ├── BookSourceService.cs    # 书源服务
│   ├── DownloadService.cs      # 下载服务
│   ├── CacheService.cs         # 缓存服务
│   ├── SyncService.cs          # 同步服务
│   ├── SettingsService.cs      # 设置服务
│   ├── HotKeyService.cs        # 全局热键服务
│   ├── TrayIconService.cs      # 托盘图标服务
│   └── StatisticsService.cs    # 统计服务
│
├── Data/                       # 数据访问层
│   ├── YuYueDbContext.cs       # EF Core 数据库上下文
│   ├── Repositories/           # 仓储模式
│   │   ├── IRepository.cs      # 仓储接口
│   │   ├── BookRepository.cs   # 书籍仓储
│   │   └── ProgressRepository.cs # 进度仓储
│   └── Migrations/             # 数据库迁移文件
│
├── Parsers/                    # 书源解析器
│   ├── IBookParser.cs          # 解析器接口
│   ├── BaseParser.cs           # 解析器基类
│   └── Parsers/                # 具体解析器实现
│       ├── QidianParser.cs     # 起点中文网
│       ├── ZonghengParser.cs   # 纵横中文网
│       └── GenericParser.cs    # 通用解析器
│
├── Helpers/                    # 辅助工具类
│   ├── WindowHelper.cs         # 窗口辅助类
│   ├── FileHelper.cs           # 文件操作辅助类
│   ├── NetworkHelper.cs        # 网络请求辅助类
│   ├── TextHelper.cs           # 文本处理辅助类
│   └── LogHelper.cs            # 日志辅助类
│
├── Controls/                   # 自定义控件
│   ├── ReaderControl.xaml      # 自定义阅读器控件
│   ├── PageFlipControl.xaml    # 翻页动画控件
│   └── ProgressBar.xaml        # 自定义进度条
│
├── Converters/                 # 数据转换器（用于绑定）
│   ├── BoolToVisibilityConverter.cs
│   ├── ColorToBrushConverter.cs
│   └── ByteToImageConverter.cs
│
├── Themes/                     # 主题样式
│   ├── LightTheme.xaml         # 浅色主题
│   ├── DarkTheme.xaml          # 深色主题
│   ├── EyeCareTheme.xaml       # 护眼主题
│   └── GenericStyles.xaml      # 通用样式
│
├── Resources/                  # 资源文件
│   ├── icon.ico                # 应用图标
│   ├── Images/                 # 图片资源
│   ├── Fonts/                  # 字体文件
│   └── Templates/              # 伪装界面模板
│
└── app.manifest                # 应用程序清单（DPI设置等）
```

## 核心类职责说明

### ViewModels（视图模型）
- 实现 `INotifyPropertyChanged` 接口（或使用 CommunityToolkit.Mvvm）
- 不包含UI逻辑，只包含数据和命令
- 通过数据绑定与View通信

### Services（服务）
- 单例模式（通过依赖注入）
- 处理业务逻辑和数据操作
- 不直接操作UI

### Models（模型）
- POCO类（简单数据对象）
- 可包含数据验证逻辑
- EF Core实体类

### Views（视图）
- 纯XAML界面，最少的代码后置
- 通过Binding绑定ViewModel
- 处理UI事件（如窗口加载、按钮点击等）

## 开发顺序建议

1. **Phase 1: 基础架构**
   - [ ] 创建Models（Book、Chapter、ReadingProgress等）
   - [ ] 创建DbContext和数据库
   - [ ] 创建基础Services（SettingsService）
   - [ ] 设计主题样式（Themes）

2. **Phase 2: 核心界面**
   - [ ] 设计MainWindow（主窗口框架）
   - [ ] 创建ReaderView（阅读器界面）
   - [ ] 创建BookshelfView（书架界面）
   - [ ] 实现基础的MVVM绑定

3. **Phase 3: 核心功能**
   - [ ] 实现本地TXT文件导入
   - [ ] 实现阅读进度保存
   - [ ] 实现翻页功能
   - [ ] 实现主题切换

4. **Phase 4: 高级功能**
   - [ ] 实现全局热键
   - [ ] 实现托盘图标
   - [ ] 实现书源系统
   - [ ] 实现窗口伪装

## 设计模式

- **MVVM**: View - ViewModel - Model 分离
- **Repository**: 数据访问抽象
- **Singleton**: Services使用单例
- **Factory**: Parser工厂模式
- **Observer**: 事件驱动（PropertyChanged）

## 依赖注入

使用 `Microsoft.Extensions.DependencyInjection` 管理服务生命周期：
- **Singleton**: Services（全局唯一）
- **Transient**: ViewModels（每次创建新实例）