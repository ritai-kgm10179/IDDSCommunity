# IDDS Community 入侵偵測與主動防護系統 - 使用與安裝說明文件

歡迎使用 **IDDS Community 3.0.0**！本文件提供系統安裝、管理介面操作與安全網路設定之完整導覽。

---

## 1. 系統架構簡介

IDDS Community 為基於 .NET 10 構建之高效能 Windows 主機層級入侵偵測與主動防護系統，包含以下三個核心元件：

1. **IDDS Community Protection Service (`IDDSCommunity.IntrusionDetection.Service.exe`)**：
   - Windows 後台服務，負責事件訂閱、日誌監控、暴力破解攻擊偵測與自動防火牆封鎖。
2. **IDDS Community Admin Console (`IDDSCommunity.IntrusionDetection.Admin.exe`)**：
   - 正體中文 GUI 管理介面，提供即時狀態監控、安全網路清單管理、事件記錄與報表匯出功能。
3. **Setup 安裝程式 (`Setup.exe`)**：
   - 整合式獨立安裝/升級/修復/移除工具，支援版本自動識別與捷徑管理。

---

## 2. 系統安裝與升級

### 2.1 全新安裝 (Fresh Installation)
1. 以 **系統管理者權限 (Run as Administrator)** 執行 `Setup.exe`。
2. 勾選需求項目：
   - `[x] 建立桌面捷徑`
   - `[x] 建立開始功能表捷徑`
3. 點擊 **「安裝」** 按鈕，安裝程式將自動部署至 `C:\Program Files\IDDS Community` 並啟動背景服務。

### 2.2 升級與修復 (Upgrade & Reinstall)
- 當執行新版本 `Setup.exe` 時，系統會自動偵測已安裝之版本：
  - **升級**：顯示「升級至 v3.0.0」並無縫更新服務與代理程式資產。
  - **降級警告**：若嘗試安裝較舊版本，系統將彈出警告對話框要求確認，防止誤將系統降級。

---

## 3. 管理控制台 (Admin Console) 使用指南

1. **總覽 (Overview)**：檢視目前服務狀態、運作中之安全性代理程式（如 Windows Network Logon、RDP、SQL Server、SSH 等）與即時封鎖統計。
2. **安全網路 (Allowed IPs)**：
   - 設定全域白名單，支援單一 IPv4/IPv6 及 CIDR 網段。
   - 本機迴路位址已由服務層自動辨識防護，無需重複輸入。
3. **報表匯出 (Report Export)**：
   - 支援將攻擊事件日誌與封鎖紀錄匯出為 JSON / CSV 報表檔。

---

## 4. 解除安裝 (Uninstallation)

若需移除系統：
1. 執行 `Setup.exe` 點擊 **「解除安裝」**，或透過 Windows 控制台移除。
2. 安裝程式將自動停止服務、刪除防火牆規則並乾淨清理桌面與開始功能表捷徑。
