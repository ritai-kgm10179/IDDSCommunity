# IDDS Community 使用者指南

本指南適用於 IDDS Community 3.0.0。IDDS Community 會建立 Windows 服務、讀取事件記錄或服務日誌，並在攻擊來源達到門檻時修改 Windows 防火牆規則。正式啟用前，請先在隔離環境完成測試。

## 1. 安裝前準備

- 使用與伺服器架構相符的 GitHub Release 安裝包：一般 Intel／AMD Windows 使用 `win-x64`，Windows on Arm 使用 `win-arm64`。
- 正式安裝包為自帶 .NET Runtime，不需另外安裝 .NET 10 Runtime。
- 使用具備安裝 Windows 服務與修改防火牆權限的帳號執行安裝程式。
- 確認要保護的服務已啟用必要的事件記錄或文字日誌。
- 記下管理主機、監控平台及內部健康檢查來源的固定 IP 或 CIDR 網段。

## 2. 首次啟動

1. 安裝後啟動「IDDS Community 入侵偵測防禦系統」。
2. 若上方顯示「找不到服務」，先按「安裝服務」並接受 UAC 提示。
3. 進入「設定 > 安全網路」，先加入管理主機的單一 IP 或 CIDR 網段，例如 `192.0.2.10` 或 `192.0.2.0/24`。
4. 回到「代理程式」，只啟用本機實際存在且已完成日誌設定的 Agent。
5. 儲存設定後啟動服務；若匯入設定或變更需要重新載入的 Agent 設定，請重新啟動服務。

> 請先建立安全網路允許清單，再啟用硬封鎖。不要從唯一的遠端管理來源進行封鎖測試。

## 3. 封鎖設定

- 軟封鎖與硬封鎖門檻是登入失敗次數；期間分別以分鐘與小時計算。
- 共用驗證型 Agent 預設以每一來源 IP 的 `10 次／5 分鐘` 滑動時間窗偵測異常，再交由封鎖政策處理。
- 永久硬封鎖應只在來源辨識可靠且已有復原管道時啟用。
- 「覆寫設定」只影響目前選取的 Agent；未勾選時使用全域封鎖設定。
- 安全網路支援 IPv4、IPv6、單一位址與 CIDR。請避免加入過大的網段，以免讓不受信任來源繞過防護。

## 4. Agent 支援矩陣

| Agent | 主要偵測來源 | 啟用前確認事項 |
| --- | --- | --- |
| FTP | 設定連接埠上的明文 FTP 回應 | TLS 加密後無法解析；確認連接埠與實際服務一致。 |
| SMTP／POP3／IMAP | 設定連接埠上的明文郵件協定回應 | STARTTLS 後及隱含 TLS 流量無法解析，應優先使用伺服器日誌。 |
| SQL Server | Windows Application Event Log，登入失敗事件 18456 | 確認 SQL Server 會將失敗登入寫入 Windows 事件記錄。 |
| MySQL／MariaDB | Windows Application Event Log 的標準 `MySQL` 或 `MariaDB` 來源，訊息須包含 `Access denied for user` 及可解析的來源 IP | MySQL 8 可能需啟用 `log_sink_syseventlog`；MariaDB 10.4 起標準來源名稱為 `MariaDB`。使用 `syseventlog.tag` 的自訂 Provider 名稱目前不支援。 |
| PostgreSQL | 一般文字或 `jsonlog` 日誌 | 設定絕對日誌目錄與搜尋模式，並確認服務帳號可讀取。 |
| FileMaker | Windows Application Event Log，事件 661 | 確認版本使用相符事件格式。 |
| 遠端桌面 | 遠端桌面登入失敗事件／回應 | 確認 Windows 稽核原則與事件記錄已啟用。 |
| Windows OpenSSH | `OpenSSH/Operational` 或指定文字日誌 | 至少啟用一種來源；文字日誌路徑必須是絕對路徑。 |
| Windows 網路登入 | Security 事件 4625、登入類型 3 | 需要讀取 Security Log 的權限；涵蓋網路登入但不宣稱能精確判定 SMB。 |
| NPS／RADIUS | Windows NPS 拒絕存取事件 6273 | 確認 NPS 稽核與事件記錄已啟用。 |
| IIS 驗證 | IIS W3C 日誌中的 HTTP 401 | 設定日誌目錄；可限制受保護 URL 前綴。 |
| Web Security | 設定連接埠上的 Web 驗證失敗回應 | HTTPS 內容無法由明文封包解析器解密。 |
| Windows DNS | DNS Server Analytical／Audit 事件 | 只支援 Microsoft Windows DNS Server 的事件格式。 |

MySQL 官方說明 Windows Event Log 輸出可使用 `log_sink_syseventlog`，未設定 tag 時來源為 `MySQL`；MariaDB 官方說明 Windows 會將錯誤記錄寫入 Application Log，10.4 起來源為 `MariaDB`，舊版為 `MySQL`。IDDS Community 不掃描資料庫連接埠，也不把一般資料庫錯誤視為登入攻擊；只有來源名稱、拒絕登入訊息與來源 IP 同時符合時才建立事件。

- [MySQL：Error Logging to the System Log](https://dev.mysql.com/doc/refman/8.4/en/error-log-syslog.html)
- [MariaDB：Error Log](https://mariadb.com/docs/server/server-management/server-monitoring-logs/error-log)

## 5. 驗證防護是否正常

1. 確認管理介面上方服務狀態正常，且目標 Agent 顯示為啟用。
2. 從不在安全網路允許清單內的隔離測試主機，對測試帳號製造少量且受控的失敗登入。
3. 在「安全性記錄」確認事件來源、IP、時間及 Agent 正確。
4. 達到測試門檻後，在「目前封鎖」確認封鎖類型及解除時間。
5. 檢查 Windows 防火牆規則及 Windows Event Viewer，確認沒有封鎖管理網段或產生非預期錯誤。
6. 測試完成後解除測試 IP，並把門檻還原為正式值。

若事件沒有出現，先檢查服務本身是否產生日誌、事件來源名稱、服務帳號讀取權限、Agent 是否啟用，以及來源 IP 是否位於安全網路允許清單。

## 6. 日常維護

- 「設定 > 資料庫維護」可執行完整性檢查、建立及驗證備份、清理過期資料、最佳化資料庫與回收空間。
- 還原備份或回收資料庫空間前必須停止服務。
- 「設定 > 設定匯入與匯出」可移轉封鎖政策、安全網路、應用程式與 Agent 設定。預設不匯出機密資料；選擇機密資料時須使用至少 12 個字元的密碼片語。
- 「設定 > 報表匯出」可依日期範圍輸出本地化 HTML 安全性報表。
- 定期檢查應用程式記錄、Windows Event Log、資料庫備份驗證結果及磁碟可用空間。

## 7. 誤封鎖與復原

1. 若仍可使用管理介面，進入「目前封鎖」選取來源 IP 並解除封鎖。
2. 若遠端管理已中斷，使用主控台或其他安全管理通道停止 `IDDSCommunityProtection` 服務，再檢查防火牆規則與安全網路設定。
3. 修正允許清單或門檻後重新啟動服務，並確認管理來源不再被封鎖。
4. 設定損壞時，可在服務停止後使用已驗證的設定套件或 SQLite 備份復原。

不要直接刪除未知資料庫檔案或批次移除所有 Windows 防火牆規則。復原前應先保留現有設定、日誌及資料庫副本。

## 8. 移除

使用安裝目錄提供的解除安裝程序。解除安裝會要求確認、停止並移除 `IDDSCommunityProtection` 服務，再刪除程式目錄；執行前請先匯出需要保留的設定、報表及資料庫備份。
