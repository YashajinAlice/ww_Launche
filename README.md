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
