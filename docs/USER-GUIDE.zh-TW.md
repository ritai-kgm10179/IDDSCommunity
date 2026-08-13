# IDDS 社群版 - 使用與安裝說明文件

歡迎使用 **IDDS 社群版**！本文件提供系統安裝、管理控制台 (Admin Console) 介面操作、SIEM 搜尋過濾、代理程式配置與安全網路設定之完整導覽。

---

## 1. 系統架構簡介

IDDS 社群版 為基於 .NET 10 構建之高效能 Windows 主機層級入侵偵測與主動防護系統，包含以下三個核心元件：

1. **IDDS 社群版 Protection Service (`IDDSCommunity.IntrusionDetection.Service.exe`)**：
   - Windows 後台服務，負責事件訂閱、日誌監控、暴力破解攻擊偵測與自動 Windows 防火牆封鎖。
2. **IDDS 社群版 Admin Console (`IDDSCommunity.IntrusionDetection.Admin.exe`)**：
   - 正體中文 GUI 管理控制台，提供即時儀表板監控、SIEM 級日誌搜尋、安全網路清單管理與代理程式調校。
3. **Setup 安裝程式 (`Setup.exe`)**：
   - 獨立整合式安裝/升級/修復/移除工具，支援版本自動識別與全使用者捷徑管理。

---

## 2. 系統安裝、升級與解除安裝

### 2.1 全新安裝 (Fresh Installation)
1. 以 **系統管理者權限 (Run as Administrator)** 執行 `Setup.exe`。
2. 勾選需求項目：
   - `[x] 建立桌面捷徑`（自動寫入公用桌面 `C:\Users\Public\Desktop`）
   - `[x] 建立開始功能表捷徑`
3. 點擊 **「安裝」** 按鈕，安裝程式將自動部署至 `C:\Program Files\IDDS Community` 並啟動背景服務。

### 2.2 升級與修復 (Upgrade & Reinstall)
- 當執行新版本 `Setup.exe` 時，系統會自動偵測已安裝之版本：
  - **升級/重新安裝**：點擊 **「重新安裝」** 或 **「升級」** 無縫更新服務與代理程式資產。
  - **降級警告**：若嘗試安裝較舊版本，系統將彈出警告對話框要求確認，防止誤將系統降級。

### 2.3 解除安裝 (Uninstallation)
1. 執行 `Setup.exe` 點擊 **「解除安裝」**（當系統已安裝時顯示）。
2. 安裝程式將自動停止 Windows 服務、刪除建立之防火牆規則並乾淨清理公用桌面與開始功能表捷徑。

---

## 3. 管理控制台 (Admin Console) 深度使用指南

啟動 **IDDS 社群版 管理控制台 (`IDDSCommunity.IntrusionDetection.Admin.exe`)**，主畫面左側為功能導引選單，包含以下 8 大核心功能面板：

### 3.1 📊 儀表板 (Dashboard / 總覽)
- **服務狀態控制**：頂部顯示目前背景服務運轉狀態 (已啟動／已停止)。若未啟動，可點擊「啟動服務」按鈕控制服務。
- **即時攻擊威脅圖表**：以長條圖與圓餅圖呈現各 Security Agent 偵測到的攻擊嘗試與封鎖次數。
- **即時防護統計**：顯示目前受保護代理程式總數、動態硬鎖定 IP 數、軟鎖定 IP 數及累積攔截攻擊次數。

### 3.2 🔎 安全性記錄 (Security Log & SIEM Search)
提供高效能 SIEM 級攻擊日誌查詢與多維度過濾功能：
- **CIDR 網段模糊搜尋**：
  - 在搜尋框輸入單一 IP（如 `192.168.1.50`）或 **IPv4 CIDR 網段（如 `192.168.1.0/24`）**，系統將自動解析出符合該網段內的所有攻擊日誌。
- **複合事件狀態過濾**：
  - 提供事件狀態勾選方塊：`[x] 入侵嘗試`、`[x] 軟鎖定`、`[x] 硬鎖定`、`[x] 系統`，支援單選或多選組合篩選。
- **代理程式選單篩選**：
  - 可選擇「全部代理程式」或特定模組（如 `RDP` 或 `OpenSSH`）精確定位問題。
- **250ms 防抖搜尋與雙重緩衝**：
  - 搜尋框內建 250ms 防抖遲延 (Debounce Timer)，避免高頻打字造成介面卡頓；DataGrid 全面啟用雙重緩衝 (DoubleBuffering)，滾動零閃爍。

### 3.3 🔒 目前封鎖 (Current Locks & Manual Relief)
管理因多次登入失敗而被系統暫時或永久封鎖的來源 IP：
- **檢視鎖定 IP**：列出受封鎖位址、觸發代理程式、鎖定狀態 (Soft Lock / Hard Lock)、剩餘鎖定時間與觸發時間。
- **手動解除封鎖 (Remove Lock)**：
  - 選取特定 IP 後點擊「解除封鎖」，系統將即時解除鎖定並自 Windows 防火牆規則中移除該位址。
- **硬鎖定轉換**：選取軟鎖定條目，可手動將其提升為強制防火牆硬鎖定。

### 3.4 🛡️ 安全網路 (Safe Networks / 白名單)
維護永遠不被封鎖的安全 IP 與網段（管理主機、內部 Gateway 等）：
- **新增允許位址**：支援輸入單一 IPv4、單一 IPv6，或 IPv4 CIDR (`192.168.0.0/16`) / IPv6 CIDR (`fe80::/10`)。
- **本機迴路自動保護**：本機 IPv4 (`127.0.0.1`) 與 IPv6 (`::1`) 已由服務層底層自動識別保護，無需重複手動輸入。
- **白名單碰撞防護 (Whitelist Collision Exclusion Guard)**：白名單中的 IP 即使觸發任何 Agent 攻擊門檻，防火牆封鎖模組亦會自動剔除，確保管理通道永中斷。

### 3.5 ⚙️ 代理程式配置 (Agent Configuration)
針對特定服務設定失敗門檻與滑動時間窗 (Sliding Window)：
- **支援 Agent 清單**：
  - `Windows Network Logon` (SMB/網路登入 Event 4625)
  - `Remote Desktop` (RDP 登入失敗)
  - `Windows OpenSSH` (SSH 服務)
  - `IIS Authentication` & `Web Security` (W3C 401 & HTTP 攻擊)
  - `Microsoft SQL Server` / `MySQL` / `PostgreSQL` (資料庫連線失敗)
  - `Mail Server` (POP3 / IMAP / SMTP 驗證失敗)
  - `NPS / RADIUS Server` & `Windows DNS Server` & `FileMaker Server`
- **門檻調校**：
  - 可獨立設定個別 Agent 之「失敗次數上限」（例如 `5 次`）與「計算時間窗」（例如 `300 秒`）。

### 3.6 🚨 防護政策設定 (Lockout Policy)
控制攻擊觸發後的階梯式防禦反應：
- **軟鎖定 (Soft Lock)**：當達到初級門檻時，暫時記憶體鎖定該 IP 一定時間（如 `15 分鐘`），期間拒絕該 IP 的特定服務請求。
- **硬鎖定 (Hard Lock)**：當累積失敗達到硬鎖定門檻或軟鎖定期間持續攻擊時，觸發 Windows 防火牆 API 建立實體封鎖規則（規則名稱前綴為 `IDDS Community`）。

### 3.7 📧 SMTP 告警與通知 (SMTP Notifications)
當系統觸發硬鎖定或嚴重事件時自動發送 Email 告警：
- **發信設定**：設定 SMTP 伺服器、連接埠、SSL/TLS 加密、寄件者與收件者信箱。
- **測試郵件**：點擊「發送測試郵件」即時驗證 SMTP 設定是否正確。
- **加密設定匯出**：設定檔匯出時，SMTP 密碼採用 **Argon2id 金鑰衍生 + AES-256-GCM 高強度加密**，確保機密不外洩。

### 3.8 🧹 資料庫維護與極致壓縮 (Database Maintenance)
管理 SQLite 歷史日誌、空間回收與完整性維護：
- **自動日誌清理 (Retention)**：背景服務預設配置 24 小時自動維護作業，將超過保留天數的舊日誌分批清理（符合 PCI DSS v4.0 規範）。
- **手動安全備份與驗證**：點擊「建立可驗證備份」自動建立 ChaCha20-Poly1305 加密且經 SHA-256 驗證之 SQLite 備份檔；提供獨立的「驗證選取的備份」動作確保檔案完整可用。備份沿用本機 DPAPI 保護的資料庫金鑰，跨機災難復原需配合金鑰保存程序。
- **實體空間壓縮 (Vacuum / Compact)**：點擊「回收資料庫空間」，執行 `PRAGMA optimize` 與 `VACUUM`，實體釋放已刪除資料所佔用的磁碟空間（執行前會自動建立防護回滾副本）。
- **完整在地化維護歷程**：歷程清單支援完整的正體中文 i18n 轉譯（如 `資料庫保留清理`、`資料庫空間回收`、`成功` 等），並維持底層 Audit Log 事件碼的一致性。

---

## 4. 常見問題與故障排除 (FAQ)

- **Q: 誤封鎖自己的管理主機 IP 該如何處置？**
  - **A**: 啟動控制台進入「目前封鎖」，找到目標 IP 點擊「解除封鎖」；隨後請務必至「安全網路」頁面將該 IP 或 CIDR 網段納入允許清單。
- **Q: 為什麼防火牆封鎖規則沒有生效？**
  - **A**: 請確認 `IDDSCommunityProtection` Windows 服務正常運作，且執行帳戶具備管理 Windows 防火牆之權限。
- **Q: 如何安全備份與轉移設定檔？**
  - **A**: 在管理控制台中點擊「設定 > 匯出設定」，系統會產生加密的 `.json` 套件；至新伺服器安裝後選擇「匯入設定」即可在一秒內完成復原。
