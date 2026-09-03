# win-xinai-de-tools

中文是默认说明语言。win-xinai-de-tools 是一个原生 Windows 桌面网络与端口管理工具，用于管理 Windows Defender 防火墙端口规则和本机网络配置。项目使用 WinUI 3、Windows App SDK 和 Windows Community Toolkit，并提供中文、英文双语界面。

## 当前版本

**v1.3.0**

稳定版下载：[GitHub Releases](https://github.com/logdns/PortManager/releases/tag/v1.3.0)

## 功能

- 添加 TCP、UDP 或 ANY 端口规则，支持入站、出站和双向方向。
- 查看已启用的防火墙规则，支持按名称和端口筛选。
- 按本地端口或远程端口查询规则。
- 删除规则并进行二次确认，同时记录审计日志。
- 监控 TCP/UDP 活动连接，显示进程名、PID 和端点。
- 使用 JSON 文件导入和导出防火墙规则备份。
- 配置网卡 IPv4、DHCP、DNS、网关和默认路由。
- 查看规则和导入导出操作的审计日志。
- 最小化到 Windows 通知区域，可从托盘菜单恢复。
- 运行时切换简体中文和英文界面。

## 安装和使用

- Windows 10 版本 1809（Build 17763）或更高版本。
- 读取和修改 Windows 防火墙规则及网络配置需要管理员权限。
- 必须下载与系统架构匹配的版本：x86、x64 或 ARM64。

程序为免安装、自包含版本，不需要另外安装 .NET 或 Windows App SDK。

### 免安装版

1. 下载对应架构的 ZIP 文件。
2. 将完整 ZIP 文件解压到本地目录。
3. 右键 `win-xinai-de-tools.exe`，选择“以管理员身份运行”。

### 安装版

1. 下载对应架构的 `win-xinai-de-tools-Setup-*.exe`。
2. 按安装向导完成安装。
3. 从开始菜单或安装目录启动 win-xinai-de-tools。

### 下载文件

| 架构 | 免安装版 | 安装版 |
|---|---|---|
| x86 | [win-xinai-de-tools-win-x86.zip](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-win-x86.zip) | [win-xinai-de-tools-Setup-x86.exe](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-Setup-x86.exe) |
| x64 | [win-xinai-de-tools-win-x64.zip](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-win-x64.zip) | [win-xinai-de-tools-Setup-x64.exe](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-Setup-x64.exe) |
| ARM64 | [win-xinai-de-tools-win-arm64.zip](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-win-arm64.zip) | [win-xinai-de-tools-Setup-arm64.exe](https://github.com/logdns/PortManager/releases/download/v1.3.0/win-xinai-de-tools-Setup-arm64.exe) |

## 从源码构建

必须在 Windows 上构建，因为 WinUI 3、Windows App SDK 和 Windows SDK 依赖 Windows 桌面工具链。

构建环境：

- Visual Studio 2022，安装 .NET 桌面开发和 Windows App SDK 工作负载。
- .NET 8 SDK。
- Windows SDK 10.0.19041.0 或更高版本。

使用 Visual Studio：

1. 打开 `PortManager.csproj`。
2. 还原 NuGet 包。
3. 选择 `x86`、`x64` 或 `ARM64` 平台。
4. 以管理员身份构建或运行项目。

命令行发布 x64 免安装版：

```powershell
dotnet publish PortManager.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:Platform=x64 `
  -p:WindowsPackageType=None `
  --output artifacts\portable\win-x64
```

将 `win-x64` 和 `Platform=x64` 替换为 `win-x86`/`x86` 或 `win-arm64`/`ARM64`，即可构建其他架构。

运行测试：

```powershell
dotnet test PortManager.Tests\PortManager.Tests.csproj --configuration Release
```

Windows 防火墙集成测试会修改防火墙状态，必须在提升权限的 Windows 会话中运行。

## GitHub Actions

工作流文件为 `.github/workflows/build.yml`，支持 Pull Request、`v*` 标签和手动运行，包含：

- 单元测试和 Windows 防火墙集成测试。
- x86、x64、ARM64 三架构构建。
- x64 GUI 启动、原生窗口图标和托盘初始化冒烟测试。
- 仅保留中文和英文资源的免安装 ZIP 打包。
- Inno Setup 安装包构建。
- 推送版本标签后自动发布 GitHub Release。

发布新版本时，更新 `PortManager.csproj` 中的版本号，提交后创建并推送标签：

```powershell
git tag vX.Y.Z
git push origin vX.Y.Z
```

## 项目结构

```text
PortManager.csproj             应用和发布配置
App.xaml(.cs)                  WinUI 应用入口和本地化
MainWindow.xaml(.cs)           导航、托盘和窗口生命周期
Views/                         功能页面和界面代码
Services/                      防火墙、连接、传输、审计和托盘服务
Models/                        领域模型和传输模型
PortManager.Tests/             单元测试和 Windows 集成测试
Properties/PublishProfiles/    x86、x64、ARM64 发布配置
installer/                     Inno Setup 安装包定义
.github/workflows/             CI、打包和发布工作流
Assets/                        图标和应用资源
```

## 权限与安全

程序请求管理员权限是因为修改 Windows 防火墙规则、IP、DNS、网关和路由需要提升权限。应用网络配置前会显示确认提示，修改后网络连接可能暂时中断。规则导入应只使用可信的 JSON 文件。连接监控只读取本机网络和进程信息，不会将数据发送到远程服务。

安全问题请通过仓库维护者私下报告，不要在公开 Issue 中发布可利用细节。

## 参与贡献

欢迎提交 Issue、功能建议和 Pull Request。请附上 Windows 版本、系统架构、程序版本、复现步骤，以及 `%LOCALAPPDATA%\PortManager\startup.log` 或 `audit.log` 中的相关内容。公开日志前请移除隐私信息。

详细规范请参阅 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 开源协议

本项目使用 [MIT License](LICENSE) 开源。

---

# win-xinai-de-tools (English)

win-xinai-de-tools is a native Windows desktop utility for managing Windows Defender Firewall port rules and local network settings. It is built with WinUI 3, Windows App SDK, and Windows Community Toolkit, with Simplified Chinese and English interfaces.

## Current Version

**v1.3.0**

Stable release: [GitHub Releases](https://github.com/logdns/PortManager/releases/tag/v1.3.0)

## Features

- Add TCP, UDP, or ANY rules with inbound, outbound, or bidirectional direction.
- List enabled firewall rules with name and port filtering.
- Query rules by local or remote port.
- Delete rules with confirmation and audit logging.
- Monitor active TCP/UDP connections, including process name, PID, and endpoints.
- Import and export firewall rule backups as JSON.
- Configure adapter IPv4, DHCP, DNS, gateway, and the default route metric.
- Review audit logs for rule and transfer operations.
- Minimize to the Windows notification area and restore from the tray menu.
- Switch between Simplified Chinese and English at runtime.

## Installation and Usage

- Windows 10 version 1809 (build 17763) or later.
- Administrator privileges are required to read or change Windows Firewall rules and network settings.
- Download the package matching the system architecture: x86, x64, or ARM64.

The application is unpackaged and self-contained. .NET and Windows App SDK do not need to be installed separately.

Portable usage:

1. Download the ZIP for the matching architecture.
2. Extract the complete archive to a local directory.
3. Run `win-xinai-de-tools.exe` as administrator.

Installer usage:

1. Download the matching `win-xinai-de-tools-Setup-*.exe`.
2. Complete the installation wizard.
3. Start win-xinai-de-tools from the Start menu or installation directory.

## Build From Source

Build on Windows because WinUI 3, Windows App SDK, and the Windows SDK require the Windows desktop toolchain. Install Visual Studio 2022 with the .NET desktop and Windows App SDK workloads, .NET 8 SDK, and Windows SDK 10.0.19041.0 or newer.

Open `PortManager.csproj`, restore NuGet packages, select `x86`, `x64`, or `ARM64`, and build or run as administrator. The command-line publish and test commands are shown in the Chinese section above.

## GitHub Actions

`.github/workflows/build.yml` runs on pull requests, `v*` tags, and manual dispatch. It runs tests, builds all three architectures, performs x64 GUI and tray smoke tests, creates portable ZIP archives and Inno Setup installers, and publishes GitHub Releases for version tags.

## Security, Contributing, and License

Firewall and network configuration changes require administrator privileges. The app asks for confirmation before applying IP, DNS, gateway, or route changes, which may briefly interrupt connectivity. Import only trusted JSON files. Connection monitoring reads local network and process information and does not transmit it remotely.

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution requirements. win-xinai-de-tools is released under the [MIT License](LICENSE).
