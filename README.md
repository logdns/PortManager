# Port Manager / 电脑端口管理 - WinUI 3

基于 WinUI 3 (Windows App SDK) 的 Windows 防火墙端口管理桌面应用，提供可切换的中文/英文界面和可扩展的功能区域。

Windows Firewall port manager built with WinUI 3. The app includes switchable Chinese and English interfaces and a reserved extension area for future modules.

## 功能

| 功能 | 说明 |
|------|------|
| 添加端口 | 支持 TCP / UDP / ANY 协议，入站使用本地端口，出站使用远程端口 |
| 端口列表 | 查询所有已启用的端口放行规则，支持名称和端口搜索 |
| 删除规则 | 按名称精确匹配删除，带二次确认 |
| 端口查询 | 按本地或远程端口查询对应的防火墙规则 |
| 连接监控 | 读取 TCP/UDP 活动连接并关联进程、PID 和端点 |
| 规则导入导出 | 以 JSON 文件备份和恢复规则 |
| 审计日志 | 记录规则查询、添加、删除及传输操作 |
| 界面语言 | 中文与英文即时切换，不混排两种语言 |
| 关于 | 版本信息与技术栈说明 |

## 技术栈

- **Microsoft UI XAML / WinUI 3** (Windows App SDK 1.8+)
- **Windows Community Toolkit 8.2** (`SettingsCard` / `SettingsExpander`)
- **.NET 8** / C# 12
- **Fluent Design** + Mica 材质
- **Windows Firewall COM API**（`HNetCfg.FwPolicy2` / `FWRule`）

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

将 `win-x64` 替换为 `win-x86` 或 `win-arm64` 即可生成对应架构。GitHub Actions 会先执行单元测试，再构建三种架构的 portable ZIP 和 Inno Setup 安装包，并在推送 `v*` 标签时创建 GitHub Release。发布流程会移除中文和英文之外的 .NET 卫星资源目录。

发布产物：

| 架构 | 免安装 | 安装包 |
|---|---|---|
| x86 | `PortManager-win-x86.zip` | `PortManager-Setup-x86.exe` |
| x64 | `PortManager-win-x64.zip` | `PortManager-Setup-x64.exe` |
| ARM64 | `PortManager-win-arm64.zip` | `PortManager-Setup-arm64.exe` |

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
│   ├── ComingSoonPage       # 更多功能入口
│   ├── ConnectionMonitorPage # 活动连接监控
│   ├── RuleTransferPage      # JSON 规则导入导出
│   ├── AuditLogPage          # 审计日志
│   ├── AddPortPage           # 添加端口
│   ├── ListRulesPage         # 端口列表
│   ├── DeleteRulePage        # 删除规则
│   ├── PortStatusPage        # 端口查询
│   └── AboutPage             # 关于
├── Models/                   # 防火墙规则、连接和传输模型
├── Services/                 # 防火墙、连接、传输和审计服务
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
                                        └── Windows Firewall COM API
                                            (HNetCfg.FwPolicy2 / FWRule)
```

`FirewallService` 封装了所有防火墙操作：
- `ListRulesAsync()` — 原生 COM 枚举并短时缓存规则
- `AddRuleAsync()` — 原生 COM 添加规则
- `DeleteRuleAsync()` — 原生 COM 删除规则
- `QueryPortAsync()` — 按端口过滤查询
- `ImportRulesAsync()` — 通过原生 COM 批量恢复规则

ANY 协议自动拆成 TCP + UDP 两条规则分别添加，保证端口条件明确且可分别管理。
