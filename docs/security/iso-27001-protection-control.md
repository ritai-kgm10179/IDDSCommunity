# Cyberarms 防護控制與 ISO/IEC 27001:2022 對照

## 控制目的與範圍

Cyberarms 僅作為 Windows 主機的登入失敗監控、暴力破解偵測、自動防火牆封鎖、解除封鎖、告警及報表控制。它不取代組織的 ISMS、EDR、防毒、弱點掃描、備份、身分治理或事件應變流程。

部署負責人必須在資產清冊及適用性聲明中列出受保護的主機、網路介面、Agent、服務、白名單、封鎖門檻及控制負責人。沒有載入或啟用對應 Agent 的服務，不得宣稱受到 Cyberarms 保護。

## Annex A 支援關係

| 控制措施 | Cyberarms 提供的功能 | 必要證據 |
| --- | --- | --- |
| A.8.15 Logging | Windows Event Log、入侵紀錄及 `ProtectionAuditLog` | 事件匯出、保存期間與檢視紀錄 |
| A.8.16 Monitoring activities | Agent 登入監控、封包計數及健康檢查 | 啟用 Agent 清單、健康狀態與定期測試結果 |
| A.8.20 Network security | Windows Firewall 自動封鎖及解除封鎖 | 隔離測試結果、規則清單及復原證據 |
| A.8.21 Security of network services | FTP、郵件、資料庫、RDP 與 Web Agent | 每項服務的適用範圍及測試案例 |
| A.5.24 至 A.5.28 | 告警、報表及可匯出的防護操作紀錄 | 事件處理程序、工單或 SIEM 關聯識別碼 |
| A.8.8 | 降低密碼猜測與暴力破解曝險 | 風險評鑑、門檻核准與誤判檢討 |

上述關係表示「支援控制措施」，不表示 Cyberarms 或部署組織已取得 ISO/IEC 27001 認證。

## 安全設定基線

- 管理程式使用 `requireAdministrator` manifest，只允許經核准的 Windows 系統管理員執行。
- 服務帳號只授予 Raw Socket、服務控制、Event Log、資料目錄及 Windows Firewall 所需權限。
- 外掛只能放在受 ACL 保護的 `Plugins` 目錄；禁止一般使用者寫入該目錄。
- 只啟用資產範圍內所需的 Agent，並為每個 Agent 核准軟封鎖、強制封鎖及永久封鎖門檻。
- 白名單必須有擁有者、理由、核准日期及定期複核日期；不得使用未經核准的廣泛網段。
- `Protection:AuditRetentionDays` 預設為 365 天，可設定 30 至 3650 天。組織政策要求較長期間時不得降低此值。
- SMTP 必須使用 TLS；收件人限於核准的安全營運信箱。
- 系統時間必須與組織核准的時間來源同步，所有防護稽核時間以 UTC 保存。

## 稽核證據與失效處理

`ProtectionAuditLog` 記錄執行期啟停、軟／強制封鎖、解除封鎖、人工解除要求，以及日／週／月報表寄送結果。欄位使用穩定事件代碼，並限制內容長度，避免把密碼或郵件認證資訊寫入證據。

`ProtectionAuditTrail.ExportJsonAsync` 可將指定 UTC 期間最多 10,000 筆資料匯出至外部證據儲存庫或 SIEM。SQLite 本機資料不是不可竄改儲存；正式環境必須定期匯出到具備存取控制、保存鎖定與備份的外部系統。

若資料庫、Firewall、Event Log、Agent、報表或稽核記錄失敗，必須產生錯誤事件並依組織事件處理程序升級。健康檢查為 `Unhealthy` 或 `Degraded` 時，不得將該主機標示為控制有效。

## 驗收與定期複核

上線前及重大變更後，執行下列驗證：

1. `dotnet build Cyberarms.slnx` 為零警告、零錯誤。
2. `dotnet test Cyberarms.slnx` 無失敗，並說明每一個略過項目。
3. 在隔離 Windows Runner 執行 `scripts/run-privileged-windows-tests.ps1`。
4. 對每個受保護服務執行核准的失敗登入案例，確認偵測、封鎖、告警、稽核紀錄及解除封鎖。
5. 驗證白名單不會誤封，且非白名單的測試來源會在核准門檻內遭封鎖。
6. 驗證服務、資料庫、Firewall 與 SMTP 故障均能被監控平台發現。
7. 將 JSON 稽核證據送入外部保存位置並測試復原與查詢。
8. 每季檢視 Agent 範圍、門檻、白名單、誤判、漏判、丟包率及失效紀錄。

正式發布仍應遵循 [Go／No-Go 檢查表](../release/go-no-go-checklist.md)。
