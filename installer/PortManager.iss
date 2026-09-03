#ifndef SourceDir
  #error SourceDir is required
#endif
#ifndef BuildArch
  #error BuildArch is required
#endif
#ifndef AppVersion
  #define AppVersion "1.3.0"
#endif

[Setup]
AppId={{AD8E0A23-F4C1-4DA9-9F52-26E5A6D38DC7}
AppName=win-xinai-de-tools
AppVersion={#AppVersion}
AppPublisher=Fengge Network (沨哥网络)
DefaultDirName={autopf}\win-xinai-de-tools
DefaultGroupName=win-xinai-de-tools
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=win-xinai-de-tools-Setup-{#BuildArch}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\win-xinai-de-tools.exe
SetupIconFile=..\Assets\PortManager.ico

#if BuildArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif BuildArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif

[Languages]
Name: "chinesesimplified"; MessagesFile: "{#SourcePath}\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\win-xinai-de-tools"; Filename: "{app}\win-xinai-de-tools.exe"
Name: "{autodesktop}\win-xinai-de-tools"; Filename: "{app}\win-xinai-de-tools.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut / 创建桌面快捷方式"; GroupDescription: "Additional icons / 附加图标"

[Run]
Filename: "{app}\win-xinai-de-tools.exe"; Description: "Launch win-xinai-de-tools / 启动 win-xinai-de-tools"; Flags: nowait postinstall skipifsilent
