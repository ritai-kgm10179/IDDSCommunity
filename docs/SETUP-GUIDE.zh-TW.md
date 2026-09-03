# IDDS 社群版 - 安裝程式使用指南（Setup Guide）

歡迎使用 **IDDS 社群版（IDDS Community）** 安裝程式！本指南針對系統管理員提供安裝程式（Setup.exe）之操作指引，涵蓋系統需求、全新部署、無縫升級、修復安裝、降級防護、解除安裝以及常見問題排查。

---

## 1. 系統需求與前置準備

- **作業系統**：
  - Windows Server 2025、Windows Server 2022、Windows Server 2019、Windows Server 2016
  - Windows 11、Windows 10（64 位元 x64 或 ARM64）
- **執行權限**：
  - 安裝、升級與解除安裝需要具備**本機系統管理員權限（Run as Administrator）**，安裝程式會主動要求 UAC 提權確認。
- **執行階段（Runtime）**：
  - IDDS 社群版發行套件為**自包含執行環境（Self-Contained Deployment）**，安裝包已內建最佳化之 .NET 10 執行階段程式庫，目標主機無需預先手動安裝 .NET Runtime 或 SDK。
- **預設安裝目錄**：
  - 程式二進位檔目錄：%ProgramFiles%\IDDS Community（即 C:\Program Files\IDDS Community）
  - 運作資料與資料庫目錄：%ProgramData%\IDDSCommunity（即 C:\ProgramData\IDDSCommunity）

---

## 2. 安裝流程與選項說明

### 2.1 全新安裝（Fresh Installation）

1. 自官方 GitHub Release 下載對應架構之安裝壓縮檔（idds-community-3.0.0-win-x64.zip 或 idds-community-3.0.0-win-arm64.zip）並解壓縮。
2. 對 Setup.exe 按右鍵，選擇 **「以系統管理員身分執行」**。
3. 於安裝精靈介面中檢視授權協議與版本資訊。
4. 勾選所需之捷徑建立選項：
   - **建立桌面捷徑**：於公用桌面（C:\Users\Public\Desktop）建立管理主控台捷徑，讓所有登入該主機之管理員皆可存取。
   - **建立開始功能表捷徑**：於系統開始功能表（IDDS Community 資料夾）建立「IDDS Community Admin Console」與「解除安裝 IDDS Community」捷徑。
5. 點擊 **「安裝」** 按鈕。
6. 安裝程式將依序執行下列作業：
   - 自動將核心程式檔、管理主控台、代理程式擴充元件與預設設定檔部署至 %ProgramFiles%\IDDS Community。
   - 建立並啟動 Windows 核心服務：IDDSCommunityProtection（IDDS Community Protection Service）。
   - 設定服務啟動類型為「自動（延遲啟動）」，並配置自動復原（當服務意外終止時自動重啟）。
   - 初始化受 DPAPI 加密保護之 SQLite 安全資料庫（iddscommunity.db）。
   - 建立系統防火牆基礎規則群組與公用捷徑。
7. 安裝完成後，可勾選「啟動 IDDS 社群版管理主控台」並點擊「完成」立即開啟介面進行後續設定。

### 2.2 升級與修復安裝（Upgrade & Repair）

- 當主機已存在舊版或相同版本之 IDDS 社群版時，執行新版 Setup.exe，系統將自動偵測並顯示已安裝版本。
- 安裝按鈕將動態轉為 **「升級」** 或 **「重新安裝」**。
- 點擊後，安裝程式將：
  1. 安全暫停並停止執行中之 IDDSCommunityProtection 服務。
  2. 更新核心主程式、代理程式擴充元件及靜態圖資（保留使用者的歷史日誌、封鎖名單及安全網路設定）。
  3. 自動執行 SQLite 資料庫綱要之平滑向前遷移（Forward Migration）。
  4. 重新啟動背景防護服務並驗證其運行健康度。

### 2.3 降級防護機制（Downgrade Protection）

- 若嘗試以較舊版本之 Setup.exe 覆蓋安裝於已部署新版本之主機上，系統將彈出警告視窗，提示目前版本高於安裝檔版本。
- 系統將要求管理員明確確認是否執行降級，以避免資料庫綱要向下不相容造成服務啟動失敗。

### 2.4 解除安裝（Uninstallation）

1. 透過下列任一方式啟動解除安裝程序：
   - 至 Windows「設定 → 應用程式與功能」選擇「IDDS Community」點擊解除安裝。
   - 執行 %ProgramFiles%\IDDS Community\Setup.exe 並點擊 **「解除安裝」**。
   - 點擊開始功能表之「解除安裝 IDDS Community」捷徑。
2. 解除安裝程式將執行下列清理作業：
   - 安全停止並移除 IDDSCommunityProtection Windows 服務。
   - 清除所有由系統動態建立之 Windows 防火牆阻絕規則（規則群組：IDDS Community）。
   - 刪除公用桌面與開始功能表之捷徑項目。
   - 移除 %ProgramFiles%\IDDS Community 程式檔案。
3. **資料保留原則**：
   - 為防止誤刪歷史資安鑑識紀錄與黑名單，歷史資料庫與稽核日誌目錄（%ProgramData%\IDDSCommunity）預設將完整保留。如需徹底抹除，可於解除安裝後手動刪除該資料夾。

---

## 3. 自動化與指令行支援（Automation CLI）

安裝程式提供自動化驗證與 CI/CD 測試開關：

`powershell
# 執行自動化重新安裝驗證（先卸載、再全新安裝、再執行覆蓋安裝驗證）
.\Setup.exe --verify-reinstall
`

---

## 4. 常見問題與故障排除（Troubleshooting）

- **Q1: 啟動 Setup.exe 時提示「存取被拒絕（Access Denied）」？**
  - **排除方法**：請確認您是否使用「以系統管理員身分執行」啟動安裝程式。Windows 服務安裝與系統防火牆操作必須具備 Elevated Administrator 權限。
- **Q2: 安裝完成後，服務顯示「找不到服務」或無法啟動？**
  - **排除方法**：開啟 Windows 事件檢視器（Event Viewer），依序檢查 Windows 記錄 → 應用程式，尋找來源為 IDDSCommunity 之錯誤紀錄。常見原因包含防毒軟體攔截服務註冊，或通訊埠（如 REST API 8444 或 Webhook 監聽）遭其他應用程式佔用。
- **Q3: 解除安裝後，先前被封鎖的 IP 為何依然無法連線？**
  - **排除方法**：解除安裝程序會自動調用 Windows 防火牆 API 批次清除規則。若解除安裝時防火牆服務被停用，可手動開啟「進階安全 Windows 防火牆」，搜尋名稱以 Blocked by IDDS Community 為開頭的規則並予以刪除。
