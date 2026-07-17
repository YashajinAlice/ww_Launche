# 更新清單格式

遠端或本機 JSON，供 `UpdateService` 讀取。

| 欄位 | 說明 |
|------|------|
| product | 產品名稱 |
| version | 語意化版本，例如 `0.2.0` |
| downloadUrl | 安裝包 / zip 下載位址 |
| sha256 | 可選，完整性校驗 |
| mandatory | 是否強制更新 |
| releaseNotes | 更新說明 |
| publishedAt | 發佈時間（ISO 8601） |

本機測試：修改 `src/WwLauncher/update-manifest.sample.json` 的 `version` 為高於 `0.1.0`，再按「檢查更新」。
