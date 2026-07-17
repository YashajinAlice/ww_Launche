# WW Launcher

WinUI 3 + C# 啟動器（unpackaged）。UI 參考 [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)：Mica、TitleBar、NavigationView L-Pattern。

## 需求

- Windows 10 1809+ / Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## 建置與執行

```powershell
dotnet restore WwLauncher.sln
dotnet build WwLauncher.sln -c Debug -p:Platform=x64
dotnet run --project src\WwLauncher\WwLauncher.csproj -c Debug -p:Platform=x64
```

## 安裝檔（Inno Setup）

安裝精靈為**繁體中文**：使用權政策、安裝路徑、是否建立捷徑（桌面／開始功能表）。

```powershell
# 需已安裝 Inno Setup 6+，或加上 -InstallInnoIfMissing
.\scripts\build-installer.ps1
# 輸出：docs\releases\YangBao-Setup-{版本}-win-x64.exe
```

## 發佈到 GitHub Releases

每個版本請上 Releases（安裝檔 + zip 更新包）：

```powershell
.\scripts\publish-release.ps1
# 或略過重建：.\scripts\publish-release.ps1 -SkipBuild
```

`docs/update-manifest.json` 的 `downloadUrl` 應指向  
`https://github.com/YashajinAlice/ww_Launche/releases/download/v{版本}/...`

## UI

| 頁面 | 說明 |
|------|------|
| 首頁 | 啟動占位、版本、功能卡片 |
| 更新 | 檢查更新、InfoBar、manifest 說明 |
| 設定 | 淺色 / 深色 / 跟隨系統、關於 |

## 版本更新（骨架）

- 本機：`update-manifest.sample.json`
- 遠端：環境變數 `WW_LAUNCHER_UPDATE_URL`

下載與套用流程尚未實作。
