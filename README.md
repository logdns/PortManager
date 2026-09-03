# Win-XinAi-De-Tools

Win-XinAi-De-Tools is a native Windows utility for network configuration, Windows Defender Firewall rules, SMB sharing, and Windows Subsystem for Linux (WSL) management. It uses WinUI 3, Windows App SDK, and Windows Community Toolkit, with English and Simplified Chinese interfaces.

Win-XinAi-De-Tools 是一款原生 Windows 工具，用于网络配置、Windows Defender 防火墙规则、SMB 共享和适用于 Linux 的 Windows 子系统（WSL）管理。项目使用 WinUI 3、Windows App SDK 和 Windows Community Toolkit，提供英文和简体中文界面。

## Current release / 当前版本

**v1.4.2**

Download: [GitHub Releases](https://github.com/logdns/Win-XinAi-De-Tools/releases/tag/v1.4.2)  ·  下载：[GitHub Releases](https://github.com/logdns/Win-XinAi-De-Tools/releases/tag/v1.4.2)

## Features / 功能

- Native WinUI 3 desktop interface with English and Simplified Chinese runtime switching.
  原生 WinUI 3 桌面界面，支持运行时切换英文和简体中文。
- Add, search, list, and delete TCP/UDP/ANY Windows Firewall rules for inbound, outbound, or bidirectional traffic.
  添加、查询、列出和删除 TCP/UDP/ANY 防火墙规则，支持入站、出站和双向流量。
- Configure adapter IPv4, DHCP, DNS, gateway, route metric, and the default route with validation and confirmation.
  配置网卡 IPv4、DHCP、DNS、网关、路由跃点和默认路由，并提供校验及确认提示。
- Toggle SMB Direct and SMB 1.0/CIFS, with optional immediate restart, and manage the fixed `share` folder share.
  开关 SMB Direct 和 SMB 1.0/CIFS，可选择立即重启，并管理固定名称 `share` 的文件夹共享。
- Monitor active TCP/UDP connections, map them to processes, close TCP connections, or terminate a process with confirmation.
  监控 TCP/UDP 活动连接，关联进程，关闭 TCP 连接或在确认后终止进程。
- Back up and restore firewall rules as JSON and review an append-only audit log.
  使用 JSON 备份和恢复防火墙规则，并查看追加式审计日志。
- Manage WSL distributions through the native `wsl.exe` command: detect installation state, install WSL or Ubuntu, start, stop, set default, refresh, and open a terminal.
  通过原生 `wsl.exe` 管理 WSL 发行版：检测安装状态、安装 WSL 或 Ubuntu、启动、停止、设置默认、刷新和打开终端。
- Minimize to the Windows notification area and restore from the tray menu.
  最小化到 Windows 通知区域，并可从托盘菜单恢复。

### WSL integration / WSL 集成

The WSL page is implemented as a native WinUI management surface in this repository. When WSL is missing, it shows an actionable setup panel with an elevated `wsl.exe --install` action, Ubuntu installation, and Microsoft's installation help link. When WSL is installed without a distribution, the page offers to install Ubuntu and then refresh the list.

WSL 页面是本仓库中的原生 WinUI 管理界面。未安装 WSL 时，界面会显示可操作的安装面板，提供提升权限的 `wsl.exe --install`、Ubuntu 安装和微软安装帮助链接；已安装 WSL 但没有发行版时，可直接安装 Ubuntu 并刷新列表。

The workflow is feature-inspired by [owu/wsl-dashboard](https://github.com/owu/wsl-dashboard), but this project does not copy, link, bundle, or redistribute its source code or assets. The two projects remain independent.

该工作流参考了 [owu/wsl-dashboard](https://github.com/owu/wsl-dashboard) 的功能方向，但本项目没有复制、链接、捆绑或再分发其源代码和资源，两个项目彼此独立。

## Installation / 安装

Windows 10 version 1809 (build 17763) or later is required. Administrator privileges are needed for firewall, network, SMB, and WSL installation changes. Choose the package matching your architecture.

需要 Windows 10 版本 1809（Build 17763）或更高版本。修改防火墙、网络、SMB 以及安装 WSL 需要管理员权限，请选择匹配系统架构的版本。

| Architecture / 架构 | Portable ZIP / 免安装 ZIP | Installer / 安装包 |
|---|---|---|
| x86 | [Win-XinAi-De-Tools-win-x86.zip](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-win-x86.zip) | [Win-XinAi-De-Tools-Setup-x86.exe](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-Setup-x86.exe) |
| x64 | [Win-XinAi-De-Tools-win-x64.zip](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-win-x64.zip) | [Win-XinAi-De-Tools-Setup-x64.exe](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-Setup-x64.exe) |
| ARM64 | [Win-XinAi-De-Tools-win-arm64.zip](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-win-arm64.zip) | [Win-XinAi-De-Tools-Setup-arm64.exe](https://github.com/logdns/Win-XinAi-De-Tools/releases/download/v1.4.2/Win-XinAi-De-Tools-Setup-arm64.exe) |

The portable build is self-contained and does not require a separate .NET or Windows App SDK installation. Extract the ZIP and run `Win-XinAi-De-Tools.exe` as administrator. The installer creates Start menu and optional desktop shortcuts.

免安装版本为自包含程序，不需要另外安装 .NET 或 Windows App SDK。解压 ZIP 后以管理员身份运行 `Win-XinAi-De-Tools.exe`；安装包可创建开始菜单和可选桌面快捷方式。

## Build and test / 构建与测试

Build on Windows with Visual Studio 2022, the .NET desktop and Windows App SDK workloads, .NET 8 SDK, and Windows SDK 10.0.19041.0 or newer.

请在 Windows 上构建，并安装 Visual Studio 2022、.NET 桌面和 Windows App SDK 工作负载、.NET 8 SDK，以及 Windows SDK 10.0.19041.0 或更高版本。

```powershell
dotnet publish Win-XinAi-De-Tools.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:Platform=x64 `
  -p:WindowsPackageType=None `
  --output artifacts\portable\win-x64

dotnet test Win-XinAi-De-Tools.Tests\Win-XinAi-De-Tools.Tests.csproj --configuration Release
```

Replace `win-x64`/`x64` with `win-x86`/`x86` or `win-arm64`/`ARM64` for other targets. Windows firewall integration tests must run in an elevated Windows session.

将 `win-x64`/`x64` 替换为 `win-x86`/`x86` 或 `win-arm64`/`ARM64` 可构建其他架构。Windows 防火墙集成测试必须在提升权限的 Windows 会话中运行。

## GitHub Actions / 自动构建

`.github/workflows/build.yml` runs unit tests, Windows firewall integration tests, x86/x64/ARM64 builds, x64 GUI and tray smoke tests, portable ZIP packaging, Inno Setup installers, and release publication for `v*` tags.

`.github/workflows/build.yml` 会运行单元测试、Windows 防火墙集成测试、x86/x64/ARM64 构建、x64 GUI 和托盘冒烟测试、免安装 ZIP 打包、Inno Setup 安装包构建，并在推送 `v*` 标签时发布 Release。

## Repository layout / 项目结构

`Views/` contains native WinUI pages; `Services/` contains firewall, network, SMB, WSL, connection, transfer, audit, and tray services; `Models/` contains domain models; `Win-XinAi-De-Tools.Tests/` contains tests; `installer/` contains the Inno Setup definition; and `Assets/` contains application icons and resources.

`Views/` 存放原生 WinUI 页面；`Services/` 存放防火墙、网络、SMB、WSL、连接、传输、审计和托盘服务；`Models/` 存放领域模型；`Win-XinAi-De-Tools.Tests/` 存放测试；`installer/` 存放 Inno Setup 定义；`Assets/` 存放图标和资源。

## Security / 安全

Run the application as administrator only when required. SMB 1.0/CIFS is a legacy protocol with known security risks. Import only trusted JSON files. Network, process, and connection information is read locally and is not transmitted to a remote service.

仅在需要时以管理员身份运行。SMB 1.0/CIFS 是存在已知安全风险的旧协议。只导入可信的 JSON 文件。网络、进程和连接信息只在本机读取，不会发送到远程服务。

## License / 开源协议

Win-XinAi-De-Tools is released under the [MIT License](LICENSE). The native WSL page is original work in this repository and is not a derivative or distribution of `owu/wsl-dashboard`.

Win-XinAi-De-Tools 使用 [MIT License](LICENSE) 发布。本仓库的原生 WSL 页面为独立实现，不是 `owu/wsl-dashboard` 的衍生或再分发版本。

`owu/wsl-dashboard` is a separate GPL-3.0 project. Its license applies to that project only; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and its [official repository](https://github.com/owu/wsl-dashboard) for details.

`owu/wsl-dashboard` 是独立的 GPL-3.0 项目，其许可仅适用于该项目；详情请参阅 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) 和[官方仓库](https://github.com/owu/wsl-dashboard)。

Issues and pull requests should include the Windows version, architecture, application version, reproduction steps, and relevant logs from `%LOCALAPPDATA%\Win-XinAi-De-Tools\startup.log` or `audit.log`. Remove private data before publishing logs.

提交 Issue 或 Pull Request 时，请附上 Windows 版本、系统架构、程序版本、复现步骤，以及 `%LOCALAPPDATA%\Win-XinAi-De-Tools\startup.log` 或 `audit.log` 中的相关日志。公开日志前请移除隐私信息。
