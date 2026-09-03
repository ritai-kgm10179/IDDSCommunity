# IDDS 社群版 - 使用與安裝說明文件

歡迎使用 **IDDS 社群版**！本文件提供系統安裝、管理控制台 (Admin Console) 介面操作、SIEM 搜尋過濾、代理程式配置與安全網路設定之完整導覽。

---

## 1. 系統架構簡介

IDDS 社群版為基於 .NET 10 構建之高效能 Windows 主機層級入侵偵測與主動防護系統，包含以下三個核心元件：

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

啟動 **IDDS 社群版管理控制台 (`IDDSCommunity.IntrusionDetection.Admin.exe`)**，主畫面左側為功能導引選單，包含以下 8 大核心功能面板：

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
- **250ms 去彈跳搜尋與雙重緩衝**：
  - 搜尋框內建 250ms 去彈跳延遲 (Debounce Timer)，避免高頻打字造成介面卡頓；DataGrid 全面啟用雙重緩衝 (DoubleBuffering)，捲動零閃爍。

### 3.3 🔒 目前封鎖 (Current Locks & Manual Relief)
管理因多次登入失敗而被系統暫時或永久封鎖的來源 IP：
- **檢視鎖定 IP**：列出受封鎖位址、觸發代理程式、鎖定狀態 (Soft Lock / Hard Lock)、剩餘鎖定時間與觸發時間。
- **手動解除封鎖 (Remove Lock)**：
  - 選取特定 IP 後點擊「解除封鎖」，系統將即時解除鎖定並自 Windows 防火牆規則中移除該位址。
- **硬鎖定轉換**：選取軟鎖定條目，可手動將其提升為強制防火牆硬鎖定。

### 3.4 📋 系統日誌 (System Operations & Audit Log)
提供管理人員檢視系統內部防禦管線、外部資料下載與維護作業之完整稽核日誌：
- **外部資料下載稽核 (External Downloads Audit)**：
  - **外部威脅情報 (ThreatFeed.Download)**：記錄 IPsum、AbuseIPDB、Spamhaus DROP 等訂閱來源下載筆數、Bogon 過濾筆數、安全網路排除筆數與網路錯誤。
  - **Team Cymru Fullbogons 動態前綴更新 (Bogon.Update)**：記錄 IPv4 / IPv6 動態 Bogon 前綴清單下載與更新狀況。
  - **GeoIP 地理位置資料庫更新 (GeoIp.Update)**：記錄 MaxMind / DB-IP 下載狀態、檔案更新筆數與記憶體熱替換。
  - **動態 DNS FQDN 解析 (DynamicDns.Resolve)**：記錄各設定網域名稱之解析狀態與最新動態 IP 清單。
  - **跨主機叢集情報同步 (Cluster.Sync)**：記錄 Edge 邊緣節點向 Threat Hub 推播與拉取之威脅情報筆數。
- **維護與防禦事件**：
  - **傳入放行規則對齊 (`Firewall.RuleAdd` / `Firewall.RuleRemove`)**：自動追蹤內部監聽服務（合法使用者自助解鎖網頁門戶、安全 RESTful 管理 API、威脅情資中繼中心 Hub、蜜罐誘捕 Decoy 通訊埠）在 Windows 防火牆中的傳入允許規則生命週期；隨服務啟用、連接埠變更或服務停止時自動執行宣告式對齊與清理。
  - 記錄智慧假釋移轉 (`Firewall.Probation`)、防火牆解鎖 (`Firewall.Unlock`)、資料庫自動清理備份 (`Database.Maintenance`) 與服務啟動停止 (`Runtime.Start` / `Runtime.Stop`)。
- **多維度篩選與匯出**：
  - 支援依「事件類別」、「執行結果（成功/失敗）」與「關鍵字」進行快速交叉過濾。
  - 提供「匯出 CSV」功能，可將符合條件之內部作業紀錄匯出為標準 CSV 檔案以供法規遵循與資安審計。

### 3.5 🛡️ 安全網路 (Safe Networks / 白名單)
維護永遠不被封鎖的安全 IP 與網段（管理主機、內部 Gateway 等）：
- **新增允許位址**：支援輸入單一 IPv4、單一 IPv6，或 IPv4 CIDR (`192.168.0.0/16`) / IPv6 CIDR (`fe80::/10`)。
- **本機迴路自動保護**：本機 IPv4 (`127.0.0.1`) 與 IPv6 (`::1`) 已由服務層底層自動識別保護，無需重複手動輸入。
- **白名單碰撞防護 (Whitelist Collision Exclusion Guard)**：白名單中的 IP 即使觸發任何 Agent 攻擊門檻，防火牆封鎖模組亦會自動剔除，確保管理通道永中斷。

### 3.6 ⚙️ 代理程式配置 (Agent Configuration)
針對特定服務設定失敗門檻與滑動時間窗 (Sliding Window)：
- **支援 Agent 清單**：
  - `Windows Network Logon` (SMB/網路登入 Event 4625)
  - `Remote Desktop` (RDP 登入失敗)
  - `Windows OpenSSH` (SSH 服務)
  - `IIS Authentication` & `Web Security` (W3C 401 & HTTP 攻擊)
  - `Microsoft SQL Server` / `MySQL` / `PostgreSQL` (資料庫連線失敗)
  - `Mail Server` (POP3 / IMAP / SMTP 驗證失敗)
  - `通用 FTP` & `FileZilla Server` (FTP 驗證失敗)
  - `NPS / RADIUS Server` & `Windows DNS Server` & `Technitium DNS Security` & `FileMaker Server`
- **門檻調校**：
  - 可獨立設定個別 Agent 之「失敗次數上限」（例如 `5 次`）與「計算時間窗」（例如 `300 秒`）。

### 3.7 🚨 防護政策設定 (Lockout Policy)
控制攻擊觸發後的階梯式防禦反應：
- **軟鎖定 (Soft Lock)**：當達到初級門檻時，暫時記憶體鎖定該 IP 一定時間（如 `15 分鐘`），期間拒絕該 IP 的特定服務請求。
- **硬鎖定 (Hard Lock)**：當累積失敗達到硬鎖定門檻或軟鎖定期間持續攻擊時，觸發 Windows 防火牆 API 建立實體封鎖規則（規則名稱包含 `Blocked by IDDS Community` 前綴，並歸屬於 `IDDS Community` 防火牆規則群組）。

### 3.8 📧 SMTP 告警與通知 (SMTP Notifications)
當系統觸發硬鎖定或嚴重事件時自動傳送 Email 告警：
- **發信設定**：設定 SMTP 伺服器、連接埠、SSL/TLS 加密、寄件者與收件者信箱。
- **測試郵件**：點擊「傳送測試郵件」即時驗證 SMTP 設定是否正確。
- **加密設定匯出**：設定檔匯出時，SMTP 密碼採用 **Argon2id 金鑰衍生 + AES-256-GCM 高強度加密**，確保機密不外洩。

### 3.9 🧹 資料庫維護與極致壓縮 (Database Maintenance)
管理 SQLite 歷史日誌、空間回收與完整性維護：
- **自動日誌清理 (Retention)**：背景服務預設配置 24 小時自動維護作業，將超過保留天數的舊日誌分批清理（保留天數可調整，有助於支援 PCI DSS 等法規對日誌保留之要求，惟本軟體不構成官方合規聲明，實際是否符合特定法規仍須由組織自行依適用條款審查）。
- **手動安全備份與驗證**：點擊「建立可驗證備份」自動建立 ChaCha20-Poly1305 加密且經 SHA-256 驗證之 SQLite 備份檔；提供獨立的「驗證選取的備份」動作確保檔案完整可用。備份沿用本機 DPAPI 保護的資料庫金鑰，跨機災難復原需配合金鑰保存程序。
- **實體空間壓縮 (Vacuum / Compact)**：點擊「回收資料庫空間」，執行 `PRAGMA optimize` 與 `VACUUM`，實體釋放已刪除資料所佔用的磁碟空間（執行前會自動建立防護回滾副本）。
- **完整在地化維護歷程**：歷程清單支援完整的正體中文 i18n 轉譯（如 `資料庫保留清理`、`資料庫空間回收`、`成功` 等），並維持底層 Audit Log 事件碼的一致性。

### 3.10 🌐 威脅情報與跨主機叢集聯防 (Threat Intelligence & Cluster Defense)
- **分散式叢集聯防架構 (Edge / Hub Topology)**：
  - `Standalone`（獨立單機）：單機獨立防禦與訂閱情資，無需設定叢集連線。
  - `EdgeNode`（邊緣防禦節點）：**需填寫「Threat Hub 端點網址」**（如 `https://hub.example.com:8443` 或多個備援端點）與叢集 API Key；定時向 Threat Hub 雙向同步全網高危威脅清單，並主動回報本機永久封鎖事件。
  - `ThreatHub`（威脅情資中繼中心）：**無需填寫端點網址（若填寫會被系統安全忽略）**，僅需設定監聽「Threat Hub 連接埠」（預設 TCP 8443）與叢集 API Key；負責集中對外訂閱全球情報，並接收各邊緣主機連入回報與秒級情資廣播。
- **動態 IP 智慧假釋與一擊再鎖機制 (Intelligent Probation & One-Strike Relock)**：
  - 永久硬封鎖記錄經過設定週期（預設 90 天）無任何攻擊活動後，排程自動轉移至假釋觀察狀態並自 Windows 防火牆放行，預防電信商動態浮動 IP 重新指派給正常使用者之長期誤封問題。
  - 處於假釋觀察期之 IP 若再次發生任何入侵違規（1 次即觸發），立即無條件升級為永久硬封鎖。
- **主動式外部威脅情報自動訂閱 (External Threat Feeds)**：
  - 支援訂閱開源 IPsum（分級 Level 1~8）、AbuseIPDB Blacklist（提供自備 API Key 與信心度門檻）與自訂 URL 清單。
  - 情資具備 TTL 生命週期（預設 7 天），未再遭通報之外部 IP 將自動移出防火牆，防止規則無限膨脹。
- **雙層 Bogon 與安全網路防護 (Bogon Guardrails & DDNS Resolver)**：
  - 整合靜態 RFC 1918 私有 IP 硬過濾與 Team Cymru Fullbogons IPv4/IPv6 動態前綴定期同步，杜絕內網誤封。
  - 安全網路支援輸入 FQDN 網域名稱（如 `office.ddns.net`），由背景排程自動動態解析最新 IP 並維持白名單有效性。

### 3.11 💬 多渠道 Webhook 即時告警 (Webhook Notifications)
支援將入侵事件即時推播至企業常用即時通訊平台與自動化 SOC 管線：
- **支援平台**：Microsoft Teams（Adaptive Cards 1.6 格式）、Slack（Block Kit 格式）、Discord（Rich Embed 嵌入卡片）、Telegram（Bot API `sendMessage`）、Generic JSON（標準 RESTful Webhook）。
- **細緻事件觸發**：可獨立勾選軟封鎖、硬封鎖與解除封鎖事件。
- **一鍵連通性測試**：於管理控制台「設定 → 通知」中提供「傳送測試 Webhook」功能，快速驗證 Webhook 端點與網路連通性。

### 3.12 🍯 誘餌蜜罐主動防禦 (Honeypot Decoy Agent)
主動部署於未使用的通訊埠（預設 TCP 23 Telnet、2222 替代 SSH、33890 替代 RDP），引誘攻擊者探測：
- **主動誘捕與一擊必殺**：任何對誘餌通訊埠的主動 TCP 探測連線將立即觸發入侵告警並由 Windows 防火牆施加硬封鎖。
- **資訊不洩漏 (No Banner)**：接收連線後立即斷開，不傳回任何服務識別標籤，確保主機安全。
- **全域白名單聯動**：探測來源自動經過 BogonIpFilter 與安全網路檢驗，防止誤觸。

### 3.13 📊 OASIS STIX 2.1 威脅情資交換與 ISO/IEC 27001:2022 稽核報表 (STIX & ISO 27001 Compliance)
- **OASIS STIX 2.1 格式匯出**：
  - 支援將本機與叢集聯防之威脅指標匯出為標準 STIX 2.1 JSON Bundle（包含 `identity`、`indicator` 與 `report` SDO 物件），便利與外部 SIEM、MISP、OpenCTI 或 SOAR 系統對接。
- **ISO/IEC 27001:2022 附錄 A (Annex A) 合規稽核報表**：
  - 內建符合性稽核報表引擎，針對 A.5.7（威脅情報）、A.8.7（主動防禦）、A.8.15（日誌記錄）、A.8.16（活動監控）、A.8.20（網路安全）及 A.8.24（密碼學控制）產製專業 HTML 稽核報告與關鍵防護數據摘要。

### 3.14 🗺️ GeoIP 國家地理標記與區域封鎖 (Geo-fencing)
- **高效能 IP 地理位置解析**：支援 IPv4 與 IPv6 網段（CIDR 及 IP 範圍格式）高速查詢所屬 ISO 3166-1 國家代碼與名稱。
- **資料來源自動下載與本機離線快取**：支援開源或自訂 GeoIP CSV 下載 URL（預設整合開源 Country CIDR 鏡像）定期背景自動更新，並於本機 `%ProgramData%\IDDSCommunity\` 建立離線快取備援，亦可指定本機離線 CSV 檔案路徑。
- **基於國家的主動存取限制 (Geo-blocking)**：可設定特定國家代碼（如 CN、RU、KP 等）阻絕規則；受限制國家之來源一旦發生攻擊行為，立即觸發一擊永久硬封鎖（One-Strike Hard Lock）落實邊界地理圍欄防禦。

### 3.15 📡 傳統 SOC / SIEM 協定即時轉送 (Syslog & CEF Integration)
- **多格式標準支援**：
  - **RFC 5424**：現代結構化 Syslog 格式，包含標準企業 PRI、Timestamp 與 Structured-Data。
  - **RFC 3164**：傳統 BSD Syslog 格式，相容各式傳統網路設備與日誌收集器。
  - **ArcSight CEF (Common Event Format)**：業界標準事件格式，便利與 Splunk、IBM QRadar、Micro Focus ArcSight 無縫對接。
- **傳輸協定**：支援 UDP、TCP 與 TLS 加密傳輸，於控制台提供即時連通性測試按鈕。

### 3.16 📈 現代 Observability 監控 (Prometheus & Grafana)
- **內建 Prometheus 指標端點**：
  - 提供符合 OpenMetrics / Prometheus 標準之 `/metrics` 抓取端點（如 `idds_active_firewall_blocks`、`idds_uptime_seconds`、`idds_probation_ips_total`）。
  - 提供 `/healthz` JSON 健康狀態檢查端點。
- **彈性安全監聽**：支援管理者自訂監聽 IP（如 `0.0.0.0` 供外部 Prometheus 抓取、特定網卡或 `127.0.0.1`），並支援監控伺服器 CIDR 白名單過濾。
- **戰情儀表板範本**：於 [`assets/dashboards/idds-grafana-dashboard.json`](file:///d:/Dev/Project/Application/IDDSCommunity/assets/dashboards/idds-grafana-dashboard.json) 提供開箱即用的 Grafana 官方儀表板範本。

### 3.17 💻 官方自動化管理模組 (PowerShell Automation Module)
- 位於 [`tools/IDDSCommunity.PowerShell/`](file:///d:/Dev/Project/Application/IDDSCommunity/tools/IDDSCommunity.PowerShell/)，提供標準 PowerShell 7+ 模組：
  - `Get-IddsStatus`：查詢服務運行狀態與資料庫檔案。
  - `Get-IddsBlockedIp`：列出目前所有防火牆封鎖之 IP 清單。
  - `Get-IddsSafeNetwork` / `Add-IddsSafeNetwork` / `Remove-IddsSafeNetwork`：安全網路白名單快速管理。
  - `Export-IddsStixBundle`：命令列一鍵匯出 STIX 2.1 JSON 情資。
  - `Export-IddsIso27001Report`：命令列一鍵產製 ISO 27001 合規稽核報告。
  - `Test-IddsNotification`：批次測試通知端點連通性。

### 3.18 🔑 合法使用者自助驗證解鎖門戶 (Self-Service TOTP Unblock Portal)
當合法管理者或內部同仁因多次密碼輸入錯誤遭到防火牆封鎖時，可透過獨立專屬連接埠（預設 TCP 8088）存取內建 Web 解鎖門戶：
- **TOTP 雙因素動態驗證 (RFC 6238)**：支援搭配 Google Authenticator、Microsoft Authenticator 等標準 TOTP 應用程式。
- **即時自動解除封鎖**：驗證成功後系統立即自 Windows 防火牆放行該 IP，免除必須登入伺服器後台手動解鎖之負擔。

### 3.19 ☁️ 雲端邊界安全網路動態同步 (Cloud Perimeter Auto-Sync: AWS, Azure, Cloudflare)
- **官方 IP 區段動態抓取**：自動定期非同步抓取並解析 AWS、Microsoft Azure、Cloudflare 官方公布之最新 IP Range JSON 清單。
- **自動合併動態白名單**：將雲端服務供應商合法反向代理與 CDN IP 自動加入安全網路，防止反向代理流量遭誤封。

### 3.20 🎭 蜜帳戶欺敵與 SOAR 指令碼自動化 (Honey Accounts & SOAR Automation)
- **蜜帳戶欺敵 (Honey Accounts / Decoy Logins)**：可設定特定虛擬誘餌帳戶名稱（如 `admin`, `root`, `test`, `guest`, `superadmin`）。任何針對此類帳戶的登入嘗試將立即觸發「一擊立即硬封鎖」，跳過累計軟鎖定門檻。
- **SOAR 自訂自動化指令碼聯動 (SOAR Script Execution)**：當系統觸發重大硬封鎖或特定威脅事件時，自動呼叫管理者預先撰寫之 PowerShell 或 Batch 腳本（傳入事件來源 IP、代理程式名稱、威脅等級等參數），無縫對接企業現有資安自動化處置流程。

### 3.21 🔌 安全 RESTful 管理 API (RESTful Management API)
內建輕量化 HTTP/HTTPS REST API 伺服器（預設 TCP 8444），提供 API Key 認證與 Bearer Token 保護：
- `GET /api/v1/status`：查詢服務運行狀態與系統統計指標。
- `GET /api/v1/locks`：列出目前所有鎖定 IP 清單。
- `POST /api/v1/locks/release`：傳入 IP 參數即時解除特定 IP 之防火牆封鎖。
- `POST /api/v1/locks/block`：傳入 IP 參數強制將特定惡意來源施加永久硬封鎖。
- `GET /api/v1/safenetworks` / `POST /api/v1/safenetworks`：動態查詢與新增安全網路白名單。

### 3.22 📋 CIS Windows Server 安全基準合規掃描與取證評估 (CIS Benchmark & Forensics)
- **五大安全原則深度評估**：涵蓋帳戶安全原則、網路通訊協定原則、Windows 防火牆組態、安全性稽核原則與應用程式安全性防護。
- **即時合規評分與改善建議**：一鍵執行完整 CIS 安全掃描，即時計算合規百分比，並針對未通過項目提供詳細之改善處置指引。
- **取證報告匯出**：支援將評估結果匯出為 JSON 取證檔案，利於資安稽核存檔與合規追蹤。

---

## 4. 常見問題與故障排除 (FAQ)

- **Q: 誤封鎖自己的管理主機 IP 該如何處置？**
  - **A**: 啟動控制台進入「目前封鎖」，找到目標 IP 點擊「解除封鎖」；隨後請務必至「安全網路」頁面將該 IP 或 CIDR 網段納入允許清單。若已啟用 TOTP 自助解鎖門戶，亦可直接以手機 App 驗證解除。
- **Q: 為什麼防火牆封鎖規則沒有生效？**
  - **A**: 請確認 `IDDSCommunityProtection` Windows 服務正常運作，且執行帳戶具備管理 Windows 防火牆之權限。
- **Q: 節點設定為 Threat Hub（威脅情資中繼中心）時，需要填寫「Threat Hub 端點網址」嗎？**
  - **A**: 不需要。Threat Hub 是服務監聽端（Server），只需設定監聽連接埠（如 8443）與 API Key 供邊緣節點連入；只有邊緣節點（EdgeNode）才需要填寫 Threat Hub 的連線網址。若在 Threat Hub 誤填了網址，系統會安全忽略，不會產生任何異常。
- **Q: 如何安全備份與轉移設定檔？**
  - **A**: 在管理控制台中點擊「設定 > 匯出設定」，系統會產生加密的 `.json` 套件；至新伺服器安裝後選擇「匯入設定」即可在一秒內完成復原。
