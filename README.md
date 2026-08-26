# Port Manager / 电脑端口管理 - WinUI 3

基于 WinUI 3 (Windows App SDK) 的 Windows 防火墙端口管理桌面应用，提供中英文双语导航和可扩展的功能区域。

Windows Firewall port manager built with WinUI 3. The shell includes a bilingual navigation experience and a reserved extension area for future modules.

## 功能

| 功能 | 说明 |
|------|------|
| 添加端口 | 支持 TCP / UDP / ANY 协议，入站 / 出站 / 双向 |
| 端口列表 | 查询所有已启用的端口放行规则，支持搜索 |
| 删除规则 | 按名称精确匹配删除，带二次确认 |
| 端口查询 | 输入端口号查看该端口的防火墙状态 |
| 关于 | 版本信息与技术栈说明 |

## 技术栈

- **WinUI 3** (Windows App SDK 1.6+)
- **.NET 8** / C# 12
- **Fluent Design** + Mica 材质
- **netsh + PowerShell** 防火墙操作

## 构建环境

- Visual Studio 2022 17.10+ (推荐 2026)
- Windows App SDK 工作负载
- .NET 8 SDK
- Windows 10 1809 (Build 17763) 或更高

## 构建

1. 用 Visual Studio 打开 `PortManager.csproj`
2. 确认 NuGet 包已还原（`Microsoft.WindowsAppSDK`）
3. 选择 x64 平台
4. F5 调试运行 / Ctrl+B 生成

### 命令行发布 / CLI publish

在 Windows 上执行以下命令可生成免安装目录（管理员权限仍由清单请求）：

```powershell
dotnet publish PortManager.csproj -c Release -r win-x64 --self-contained true -p:WindowsPackageType=None -o artifacts\win-x64
```

将 `win-x64` 替换为 `win-x86` 或 `win-arm64` 即可生成对应架构。GitHub Actions 会对三种架构执行测试、构建，并上传 portable ZIP 和 MSIX 包。

## 项目结构

```
PortManager/
├── PortManager.csproj        # 项目文件
├── app.manifest              # Win32 清单（请求管理员权限）
├── Package.appxmanifest      # MSIX 包清单
├── App.xaml / .cs            # 应用入口
├── MainWindow.xaml / .cs    # 主窗口 + NavigationView 导航
├── Views/
│   ├── DashboardPage        # 双语概览与扩展入口
│   ├── ComingSoonPage       # 后续功能占位页
│   ├── AddPortPage           # 添加端口
│   ├── ListRulesPage         # 端口列表
│   ├── DeleteRulePage        # 删除规则
│   ├── PortStatusPage        # 端口查询
│   └── AboutPage             # 关于
├── Models/
│   └── FirewallRuleModel.cs  # 数据模型
├── Services/
│   └── FirewallService.cs     # 防火墙操作服务
├── PortManager.Tests/        # 规则模型单元测试
├── Properties/PublishProfiles/ # x86/x64/ARM64 发布配置
├── .github/workflows/build.yml # GitHub Actions 构建与打包
└── Assets/                   # 图标资源
```

## 架构

```
用户界面 (Views)
    │
    ├── AddPortPage ──────────┐
    ├── ListRulesPage ────────┤
    ├── DeleteRulePage ───────┼──► FirewallService (Services)
    └── PortStatusPage ───────┘         │
                                        ├── netsh.exe (添加/删除)
                                        └── powershell.exe (查询)
```

`FirewallService` 封装了所有防火墙操作：
- `ListRulesAsync()` — PowerShell `Get-NetFirewallRule` 查询
- `AddRuleAsync()` — `netsh advfirewall firewall add rule`
- `DeleteRuleAsync()` — `netsh advfirewall firewall delete rule`
- `QueryPortAsync()` — 按端口过滤查询

ANY 协议自动拆成 TCP + UDP 两条规则分别添加，规避 netsh 不支持 `protocol=any` 带端口参数的限制。
