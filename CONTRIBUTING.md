# Contributing

Thank you for helping improve Port Manager.

## Development Environment

- Windows 10 version 1809 or later.
- Visual Studio 2022 with the Windows App SDK workload.
- .NET 8 SDK.
- Administrator access for firewall integration tests and manual feature testing.

## Before Opening A Pull Request

1. Keep changes focused and preserve the existing WinUI 3 and Community Toolkit patterns.
2. Keep all user-facing strings in both `Localization/Strings.zh-CN.xaml` and `Localization/Strings.en-US.xaml`.
3. Add or update tests for service and model behavior.
4. Run `dotnet test PortManager.Tests\PortManager.Tests.csproj --configuration Release` on Windows.
5. Run `git diff --check` and confirm that generated `bin/`, `obj/`, and `artifacts/` files are not included.
6. Describe architecture-specific or administrator-permission requirements in the pull request.

## Reporting Bugs

Include the application version, Windows build, architecture, exact reproduction steps, and relevant logs from `%LOCALAPPDATA%\PortManager\startup.log` or `%LOCALAPPDATA%\PortManager\audit.log`. Remove private data before posting logs publicly.

## License

By contributing, you agree that your contributions are provided under the MIT License in this repository.
