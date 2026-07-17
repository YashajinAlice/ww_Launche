; 秧寶 Inno Setup 安裝腳本
; 需求：Inno Setup 6+（含 Languages\ChineseTraditional.isl）
; 建置：先 publish 到 publish\win-x64，再執行 scripts\build-installer.ps1

#ifndef MyAppVersion
  #define MyAppVersion "0.2.2"
#endif

#define MyAppName "秧寶"
#define MyAppPublisher "YashajinAlice"
#define MyAppURL "https://github.com/YashajinAlice/ww_Launche"
#define MyAppExeName "WwLauncher.exe"
#define PublishDir "..\publish\win-x64"

[Setup]
AppId={{A8E7C3F1-9B2D-4E6A-8F01-2C5D7A9B0E44}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=License.zh-Hant.txt
OutputDir=..\docs\releases
OutputBaseFilename=YangBao-Setup-{#MyAppVersion}-win-x64
SetupIconFile=..\src\WwLauncher\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
ShowLanguageDialog=no
LanguageDetectionMethod=uilanguage
MinVersion=10.0.17763
InfoBeforeFile=
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安裝程式
VersionInfoProductName={#MyAppName}
VersionInfoCopyright=Copyright (C) {#MyAppPublisher}

[Languages]
Name: "chinesetraditional"; MessagesFile: "i18n\ChineseTraditional.isl"

[Tasks]
Name: "desktopicon"; Description: "建立桌面捷徑"; GroupDescription: "是否建立捷徑："; Flags: checkedonce
Name: "startmenu"; Description: "建立開始功能表捷徑"; GroupDescription: "是否建立捷徑："; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu
Name: "{group}\解除安裝 {#MyAppName}"; Filename: "{uninstallexe}"; Tasks: startmenu
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即啟動 {#MyAppName}"; Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
