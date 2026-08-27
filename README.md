# Port Manager

Port Manager is a native Windows desktop utility for managing Windows Defender Firewall port rules. It is built with WinUI 3, Windows App SDK, and Windows Community Toolkit, with Chinese and English interfaces.

Port Manager 是一个原生 Windows 桌面端口管理工具，用于管理 Windows Defender 防火墙端口规则。项目使用 WinUI 3、Windows App SDK 和 Windows Community Toolkit，提供中文和英文界面。

## Features / 功能

- Add TCP, UDP, or ANY port rules with inbound, outbound, or bidirectional direction.
- List enabled firewall rules with name and port filtering.
- Query rules by local or remote port.
- Delete rules with confirmation and audit logging.
- Monitor active TCP/UDP connections, including process name, PID, and endpoints.
- Import and export firewall rules as JSON backups.
- View audit logs for rule and transfer operations.
- Minimize to the Windows notification area and restore from the tray menu.
- Switch between Simplified Chinese and English at runtime.

## Requirements / 运行要求

- Windows 10 version 1809 (build 17763) or later.
- Administrator privileges are required to read or change Windows Firewall rules.
- Use the package matching the operating system architecture: x86, x64, or ARM64.

The application is unpackaged and self-contained. It does not require .NET or Windows App SDK to be installed separately.

## Download / 下载

Stable releases are published at [GitHub Releases](https://github.com/logdns/PortManager/releases). Each release provides portable ZIP archives and Inno Setup installers for x86, x64, and ARM64.

Portable package usage:

1. Download the archive matching the Windows architecture.
2. Extract the complete archive to a local directory.
3. Run `PortManager.exe` as administrator.

Installer usage:

1. Download and run the matching `PortManager-Setup-*.exe`.
2. Complete the installation wizard.
3. Start Port Manager from the Start menu or installation directory.

## Build From Source / 从源码构建

Builds must be performed on Windows because WinUI 3 and the Windows App SDK require the Windows SDK and Windows desktop toolchain.

Prerequisites:

- Visual Studio 2022 with the .NET desktop and Windows App SDK workloads.
- .NET 8 SDK.
- Windows SDK 10.0.19041.0 or newer.

Visual Studio:

1. Open `PortManager.csproj`.
2. Restore NuGet packages.
3. Select `x86`, `x64`, or `ARM64`.
4. Build or run the project as administrator.

Command line publish:

```powershell
dotnet publish PortManager.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:Platform=x64 `
  -p:WindowsPackageType=None `
  --output artifacts\portable\win-x64
```

Replace `win-x64` and `Platform=x64` with `win-x86`/`x86` or `win-arm64`/`ARM64` as needed.

Run tests:

```powershell
dotnet test PortManager.Tests\PortManager.Tests.csproj --configuration Release
```

The Windows firewall integration test changes firewall state and requires an elevated Windows session. It is enabled by the GitHub Actions workflow and can be selected locally with the `WindowsIntegration` category.

## GitHub Actions / 持续集成

`.github/workflows/build.yml` runs on pull requests, `v*` tags, and manual dispatch. It performs:

- Unit tests and Windows firewall integration tests.
- x86, x64, and ARM64 builds.
- x64 packaged GUI startup and native icon smoke testing.
- Portable ZIP creation with only Chinese and English resources.
- Inno Setup installer creation.
- GitHub Release publication for version tags.

To publish a release, update the version in `PortManager.csproj`, commit the change, and push a semantic version tag:

```powershell
git tag v1.2.7
git push origin v1.2.7
```

## Project Layout / 项目结构

```text
PortManager.csproj             Application and package configuration
App.xaml(.cs)                  WinUI application entry point and localization
MainWindow.xaml(.cs)           Navigation shell, tray integration, and lifecycle
Views/                         Feature pages and UI code-behind
Services/                      Firewall, connection, transfer, audit, and tray services
Models/                        Domain and transfer models
PortManager.Tests/             Unit and Windows integration tests
Properties/PublishProfiles/    x86, x64, and ARM64 publish profiles
installer/                     Inno Setup installer definition
.github/workflows/             CI, packaging, and release workflow
Assets/                        Application icons and package artwork
```

## Security and Permissions / 安全与权限

The app requests administrator privileges because Windows Firewall rule changes require elevation. Rule imports should only be performed from trusted JSON files. Connection monitoring reads local networking and process information and does not transmit it to a remote service.

Please report security issues privately to the repository owner rather than opening a public issue with exploit details.

## Contributing / 参与贡献

Bug reports, feature requests, and pull requests are welcome. Please include the Windows version, architecture, application version, reproduction steps, and relevant entries from `%LOCALAPPDATA%\PortManager\startup.log` or `audit.log`.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development checklist.

## License / 开源协议

Port Manager is released under the [MIT License](LICENSE).
