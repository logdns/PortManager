# Native WSL helpers

The Rust and Go programs in this directory are small, optional Windows bridges for environments that want to host WSL command execution outside the WinUI process. Both accept the same arguments as `wsl.exe` and forward standard input, output, and exit codes.

These helpers are independent MIT-licensed code in this repository. The application uses `wsl.exe` directly by default; set `WINXINAI_WSL_HELPER` to the absolute path of either helper to opt in during development or deployment.

## Build

Rust:

```powershell
cd native\wsl-helper-rust
cargo build --release --target x86_64-pc-windows-msvc
```

Go:

```powershell
cd native\wsl-helper-go
$env:GOOS = "windows"
$env:GOARCH = "amd64"
go build -o wsl-helper-go.exe .
```

The WinUI application remains the user interface and policy layer. The helpers do not embed a shell, download distributions, or transmit data.
