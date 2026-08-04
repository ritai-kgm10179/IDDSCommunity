# Windows DNS Security Agent

## 支援平台與官方依據

本 Agent 使用 Windows Server 內建的 DNS Analytical 與 Audit Event Log，不使用已停止支援 Windows 的 BIND，也不啟用資源密集的傳統 DNS debug log。

Microsoft 官方文件確認此功能適用於 Windows Server 2016、2019、2022 與 2025：

- <https://learn.microsoft.com/windows-server/networking/dns/dns-logging-and-diagnostics>
- DNS Analytical ETW Provider：`{EB79061A-A566-4698-9119-3ED2807060E7}`

依官方事件結構，本 Agent 使用：

| Event ID | 官方事件 | Cyberarms 用途 |
| --- | --- | --- |
| 257 | Response success | 查詢速率、NXDOMAIN、ANY 查詢 |
| 258 | Response failure | 失敗回應、NXDOMAIN、ANY 查詢 |
| 263 | Dynamic update received | 動態更新速率 |
| 266 | IXFR request received | 增量區域傳送 |
| 270 | AXFR request received | 完整區域傳送 |
| 519、520 | Dynamic record create/delete audit | 動態更新稽核補充來源 |

## 部署前置條件

1. Windows Server 已安裝 DNS Server role 與 RSAT DNS 工具。
2. Cyberarms 服務帳號可讀取 `Microsoft-Windows-DNSServer/Analytical` 與 `Microsoft-Windows-DNSServer/Audit`。
3. DNS Audit Log 預設已啟用；部署人員仍須確認其狀態。
4. 依 Microsoft 文件在 Event Viewer 顯示 Analytical and Debug Logs，將 DNS-Server Analytical 設為啟用。
5. 因 Agent 使用即時 Event Log 訂閱，Analytical Log 必須使用可查詢的保存模式；若採循環模式導致查詢錯誤，Agent 啟動會失敗並留下錯誤事件。
6. 將 `Cyberarms.Agents.WindowsDns.dll` 及必要相依檔放入受 ACL 保護的 `Plugins` 目錄，再於管理介面註冊及啟用。
7. 若希望 DNS Agent 首次偵測後即交由既有封鎖流程處理，將此 Agent 的核心 `SoftLockAttempts` 設為 `1`；較高值會要求同一來源在核心累計多次 Agent 通知才封鎖。

可使用系統管理員 PowerShell 確認通道：

```powershell
Get-WinEvent -ListLog 'Microsoft-Windows-DNSServer/Analytical'
Get-WinEvent -ListLog 'Microsoft-Windows-DNSServer/Audit'
```

## 偵測設定

| 設定 | 預設值 | 說明 |
| --- | ---: | --- |
| `WindowSeconds` | 60 | 滾動計數時間窗；允許 1 至 3600 秒 |
| `QueryRateThreshold` | 1000 | 單一來源在時間窗內的 DNS 回應數 |
| `NxDomainThreshold` | 100 | 單一來源的 NXDOMAIN 回應數 |
| `AnyQueryThreshold` | 25 | 單一來源的 ANY 查詢數 |
| `DynamicUpdateThreshold` | 10 | 單一來源的動態更新數 |
| `ZoneTransferThreshold` | 1 | 單一來源的 IXFR／AXFR 要求數 |
| `MaximumTrackedClients` | 10000 | 記憶體中的最大來源數；允許 100 至 1,000,000 |
| `ExcludedAddresses` | `127.0.0.1;::1` | Agent 層不分析的精確 IP，以分號或逗號分隔 |

組織核准的 DNS Secondary、轉送站、監控系統與網域控制站還應加入 Cyberarms 全域安全網路清單。不要僅依賴 Agent 排除清單。

## 效能與邊界

Microsoft 表示 DNS Analytical logging 在約 100,000 QPS 時可能造成約 5% 效能下降，50,000 QPS 以下沒有明顯影響；正式環境仍必須監控 CPU、記憶體、事件遺失與 `Cyberarms.WindowsDns` Metrics。

本 Agent 提供主機層來源封鎖，不是上游 DDoS 清洗。若攻擊已耗盡網路頻寬，必須由 Anycast DNS、邊界防火牆或供應商流量清洗處理。

## 驗收

1. 從核准測試 IP 產生一般、NXDOMAIN 與 ANY 查詢，確認只在設定門檻交叉時產生一次偵測。
2. 從非核准來源測試 IXFR／AXFR 與動態更新。
3. 確認安全網路不會遭封鎖，非白名單測試來源會進入既有 Cyberarms 封鎖流程。
4. 驗證通知、報表、`ProtectionAuditLog`、Windows Event Log 與 Metrics。
5. 停用 Analytical channel，確認 Agent 明確啟動失敗，而不是靜默失效。
6. 以實際峰值 QPS 執行負載測試並記錄 DNS 延遲、CPU、事件遺失及 Cyberarms 丟棄數。

單元測試以 Microsoft 公開的事件訊息欄位順序建構固定資料，涵蓋 257、263、270 與 519；實機 Event Log 權限、DNS role、Analytical channel 與高流量效能仍須在目標 Windows Server 驗收，不能由開發機單元測試取代。
