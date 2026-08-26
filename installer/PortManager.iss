#ifndef SourceDir
  #error SourceDir is required
#endif
#ifndef BuildArch
  #error BuildArch is required
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{AD8E0A23-F4C1-4DA9-9F52-26E5A6D38DC7}
AppName=Port Manager
AppVersion={#AppVersion}
AppPublisher=PortManager
DefaultDirName={autopf}\Port Manager
DefaultGroupName=Port Manager
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=PortManager-Setup-{#BuildArch}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\PortManager.exe

#if BuildArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif BuildArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Port Manager"; Filename: "{app}\PortManager.exe"
Name: "{autodesktop}\Port Manager"; Filename: "{app}\PortManager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut / 创建桌面快捷方式"; GroupDescription: "Additional icons / 附加图标"

[Run]
Filename: "{app}\PortManager.exe"; Description: "Launch Port Manager / 启动端口管理器"; Flags: nowait postinstall skipifsilent
