# IDDS 社群版 (IDDS Community)

IDDS 社群版 是 Windows Server 上的社群維護入侵偵測與主動防護系統。它會由可載入的代理程式分析支援的服務事件或通訊協定失敗回應，將事件送入有界非同步處理管線，並透過 Windows 防火牆封鎖達到門檻的來源 IP。

## AI 產製聲明

本專案現行發行內容中的程式碼、文件、圖片及其他資源皆由人工智慧協作產生，並由專案維護者檢視、測試與整合。AI 產製聲明不取代各項第三方套件、歷史來源與授權文件；相關權利與授權仍以 [`LICENSE`](LICENSE)、[`LICENSE-PROVENANCE.md`](LICENSE-PROVENANCE.md) 及 [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) 為準。

UI 圖資採可重現的程式化原創產製流程，詳見 [`ASSET-PROVENANCE.md`](ASSET-PROVENANCE.md)。禁止重新加入來源不明或舊專案圖檔。

本專案源自過去於 CodePlex 發布的 IDDS 前身原始碼，但不是原權利人目前商業產品的官方版本，也未獲其贊助或背書。專案、組件與根命名空間已統一改為 `IDDSCommunity.*`；歷史名稱僅保留於授權來源紀錄。

## 主要功能

- Windows 服務型防護核心與 WinForms 管理介面。
- 硬封鎖、軟封鎖、安全網路允許清單與自動解除封鎖。
- Windows 防火牆規則管理、事件記錄、SMTP 通知，以及每日、每週與每月 HTML 報表。
- 有界 `Channel`、背壓、取消權杖、非同步服務生命週期及 UI 執行緒安全更新。
- 代理程式外掛：FTP、POP3/SMTP/IMAP、Microsoft SQL Server、MySQL／MariaDB、PostgreSQL、FileMaker、遠端桌面、Windows OpenSSH、Windows 網路登入、NPS/RADIUS、IIS 驗證、Web Security 與 Windows DNS Server。
- 共用驗證失敗偵測框架採用每一來源 IP 的滑動時間窗、事件去重、容量上限、閒置狀態 TTL 清理，以及 IPv4／IPv6 單一位址或 CIDR 排除；預設門檻為 `10 次／5 分鐘`，各代理程式可個別調整，且封鎖仍由既有漸進式政策執行。文字日誌來源只提交完整換行紀錄，並以位元組位置與檔案錨點處理半行、截斷及輪替。
- 設定頁提供版本化 JSON 匯入／匯出；預設排除密碼與機器路徑，選擇匯出 SMTP 密碼時以 Argon2id（64 MiB、3 次、單一平行度）衍生金鑰，再由 AES-256-GCM 加密及驗證。匯入前會限制密碼衍生參數、驗證套件並建立可驗證的 SQLite 安全備份，再於單一交易中套用。
- SQLite 主資料庫、WAL 與應用程式建立的維護備份採 SQLite3 Multiple Ciphers 預設的 ChaCha20-Poly1305 頁面加密。應用程式首次開啟既有明文資料庫時會先建立快照、加密並驗證後再原子替換；隨機 256 位元資料庫金鑰由 Windows DPAPI（本機範圍）保護，金鑰遺失時會拒絕建立空白資料庫，以免靜默覆蓋既有資料。
- 正體中文與英文資源；管理介面、提示、錯誤、例外訊息與報表均使用本地化資源。

## 支援界線

- 目標平台為 Windows，所有專案使用 .NET 10 `net10.0-windows`。
- 加密維護備份只能由持有相同 DPAPI 金鑰檔的同一 Windows 安裝環境還原；僅複製 `.db` 備份到另一台主機並不足以復原。此機制保護離線複製的資料檔，不宣稱能抵抗已取得本機系統管理權限或可讀取執行中程序記憶體的攻擊者。
- FTP 與明文郵件 Agent 解析設定連接埠上的協定回應，不是只要連接埠開放就能保護任意服務。TLS/SSL 或 STARTTLS 升級後的加密內容不會被封包解析器解密；應優先使用伺服器原生稽核記錄整合。
- Windows DNS Agent 專門訂閱 `Microsoft-Windows-DNSServer/Analytical` 與 `Audit`；另提供 Technitium DNS Security Agent 專屬監控 Technitium DNS 日誌。
- Agent 是否適用取決於伺服器版本、事件記錄設定、通訊協定及部署權限；上線前必須在隔離環境驗證。
- Windows 網路登入 Agent 僅處理事件 `4625`、登入類型 `3` 與高可信度認證失敗狀態；它涵蓋 SMB 等網路登入來源，但不宣稱能僅依該事件精確辨識 SMB。
- OpenSSH 預設讀取 `OpenSSH/Operational`，亦可設定文字記錄檔；PostgreSQL 支援一般文字與 `jsonlog`；IIS 讀取 W3C `401` 記錄；FileZilla 支援監看 FileZilla Server 驗證日誌檔；Technitium DNS 支援監看 Technitium 查詢/拒絕日誌檔（兩者皆支援自訂路徑與檔名模式）。
- MySQL／MariaDB Agent 讀取 Windows Application Event Log，只接受標準 `MySQL` 或 `MariaDB` 來源中同時具有 `Access denied for user` 與有效來源 IP 的事件；不掃描資料庫連接埠。MySQL 8 可能需要啟用 `log_sink_syseventlog`；設定 `syseventlog.tag` 產生的自訂 Provider 名稱目前不支援。

## 快速開始

1. 從 GitHub Release 下載與 Windows 架構相符的自帶 Runtime 安裝包並完成安裝。
2. 啟動管理介面；若顯示找不到服務，按「安裝服務」並接受 UAC 提示。
3. 先到「設定 > 安全網路」加入管理主機的 IP 或 CIDR，再啟用任何硬封鎖。
4. 到「代理程式」只啟用本機實際使用的服務，確認事件記錄或日誌來源後儲存設定。
5. 啟動服務，從隔離測試主機製造受控的失敗登入，再到「安全性記錄」及「目前封鎖」確認結果。

完整的 Agent 支援矩陣、首次設定、驗證、備份、報表、誤封鎖復原與移除步驟請參閱 [`docs/USER-GUIDE.zh-TW.md`](docs/USER-GUIDE.zh-TW.md)。

## 建置與測試

需求：Windows、.NET 10 SDK，以及可使用 Windows 特定 API 的建置環境。

```powershell
dotnet build IDDSCommunity.slnx
dotnet test IDDSCommunity.slnx
```

建立自包含安裝套件：

```powershell
.\build-setup.ps1 -RuntimeIdentifier win-x64 -Configuration Release
```

推送符合 `vX.Y.Z` 的 GPG 簽署 annotated tag 後，Release CI 會驗證標籤與專案版本、執行完整測試、建立 `win-x64` 與 `win-arm64` 自帶 Runtime 安裝包，並發布 GitHub Release。每個平台都會附上目前規格的 SPDX 3.0 SBOM、相容性較廣的 SPDX 2.2 SBOM、SHA-256 雜湊與 GitHub artifact attestation；兩種 SBOM 也會收錄於對應安裝包內。

輸出位於 `artifacts\setup\idds-community-<Version>-<RID>`。只有安裝程式與需要變更服務／防火牆的短生命週期操作要求 UAC 提權；一般 Visual Studio 偵錯及管理介面啟動不強制以系統管理員執行。

## 安裝與移除

安裝程式會顯示確認提示，將檔案部署到 `%ProgramFiles%\IDDS Community`，並透過 Windows 服務控制管理員建立 `IDDSCommunityProtection` 服務。解除安裝前同樣會要求確認，停止並移除服務後刪除該安裝目錄。

## 授權與品牌

程式碼依儲存庫根目錄的 [MIT License](LICENSE) 散布。來源證據與限制記錄於 [LICENSE-PROVENANCE.md](LICENSE-PROVENANCE.md)，第三方元件見 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)，獨立分支及商標聲明見 [FORK-NOTICE.md](FORK-NOTICE.md)。原權利人網站目前的商業 EULA 不會打包為本社群原始碼的授權文件。

新圖示與配色為本社群分支重新製作，未沿用前身的品牌圖示。名稱、商標與第三方資產仍分別受其權利人規範；正式商業散布前應由合格法律專業人員進行個案審查。

## 安全性說明

本軟體會修改 Windows 防火牆及服務狀態，屬高權限防護元件。請先備份設定、限制管理主機存取、使用最小權限服務帳號，並保留 Windows 事件記錄。MIT License 不提供任何適售性、安全性或特定用途保證。
