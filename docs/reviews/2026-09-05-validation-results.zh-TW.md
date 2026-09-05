IDDS Community 修正與驗證結果（2026-09-05）

基準：main 已由 ce0ec16 fast-forward 至 e0ca8d2081795ca2066cbdf62d9a4a5892662ff7。已實作審查列出的主要修正及後續發現的 WAF 逾時處理問題；目前變更留在工作目錄，未提交、未推送。

本文件取代初始審查中尚未成功還原套件的舊驗證狀態。完整計畫與部署注意事項分別見同目錄的 2026-09-05-remediation-plan.zh-TW.md、2026-09-05-deployment-notes.zh-TW.md。

已完成的修正

- 設定格式升版與機密加密；無機密匯入保留目的端機密。
- 管理 API 必要驗證、HTTPS、有界請求、本文大小及格式檢查；要求與完成狀態分離；自助入口可信代理與硬封鎖保護。
- AWS 官方 SDK 與完整 IP 集合更新；Azure/GCP 規則歸屬、多 IP 保留與衝突處理；HiCloud 明確回報不支援 deny。
- Hub/Edge 有界持久化、期限與增量分頁；雲端待送匣重試、版本及重啟保護；假釋使用最近活動並限永久封鎖。
- 慢速攻擊狀態上限、HLL、IPv6 動作權杖、SOAR 雙串流與取消、有限通知／HTTP／下載。
- GeoIP/Bogon 索引及完整快照保留；下載取消與停止等待；管理介面欄位說明及提供者釋放。
- WAF 改用線性時間引擎；65,536 字元上限與每規則 200 ms 期限，超限及逾時不再回報安全。全空白超長輸入同樣受限。

最終自動驗證

| 項目 | 結果 |
| --- | --- |
| 完整方案建置 | 0 警告、0 錯誤 |
| 完整方案 MSTest | 24 個測試專案，463 通過、0 失敗、5 略過，合計 468 |
| Shared | 232 通過、0 失敗 |
| Service | 91 通過、0 失敗、5 略過 |
| Setup | 40 通過、0 失敗 |
| 其他 Agent 測試 | 100 通過、0 失敗 |
| 直接與間接套件弱點 | 52 個專案，沒有回報已知弱點 |
| 直接套件最新穩定版本 | 52 個專案，沒有回報可升級項目 |
| 產物目錄擁有者 | 1,832 個 bin／obj／TestResults 目錄均為目前登入使用者；無異常擁有者 |
| 工作區格式 | 修改檔案 CRLF、規定的 UTF-8 BOM 類型與 git diff --check 通過 |
| 建置程序 | 最終檢查沒有 dotnet、MSBuild、VBCSCompiler、testhost 殘留程序 |

還原、建置與測試均以目前登入使用者的非系統管理員工作階段執行；使用單一 MSBuild 節點、停用平行還原及節點重用。套件已更新至掃描當下可取得的穩定版本，包括 AWSSDK.WAFV2 4.0.101.7、CsWin32 0.3.333、MSTest 4.4.0。弱點結果是當次資料來源的掃描結果，不代表不存在尚未揭露的弱點。

主要測試證據包括：設定機密來回與保留、IPv6 權杖與竄改、並行容量上限、真實子程序 2 MiB 雙輸出與取消終止、雲端兩個 IP 不互相覆寫、GCP 412 重試、SQLite 待送匣重啟與舊版本完成保護、Hub 持久世代／分頁／期限、近期攻擊排除假釋、軟解鎖拒絕硬封鎖要求、HTTP 未授權／無效 JSON／超大本文、下載取消與部分失敗保留快照，以及 WAF 長輸入。

效能量測

Debug、本機合成資料：8,192 個前綴、2,048 次查詢，全部方法均命中 1,024 次並核對結果一致。索引建構 25.242 ms。

| 方法 | 查詢總時間 | p95 | p99 | 查詢期間受控配置 |
| --- | --- | --- | --- | --- |
| 線性參考 | 114.408 ms | 99.500 μs | 143.800 μs | 0 bytes |
| GeoIP 索引 | 0.597 ms | 0.400 μs | 0.600 μs | 0 bytes |
| Bogon 索引 | 0.599 ms | 0.400 μs | 0.600 μs | 0 bytes |

這是隔離查詢基準，未涵蓋完整服務工作集、真實流量吞吐、磁碟 I/O 或防火牆 COM 延遲。SQLite／COM 批次仍須先取得正式負載數據，未宣稱已完成這類最佳化。

剩餘驗收與限制

- 未測試：5 項需 Windows 系統環境的整合測試，包含原始 Socket、EventLog 建立寫入刪除、防火牆規則、事件管線實際封鎖，以及 Windows Service 停止／啟動／還原。
- 未測試：真實 AWS/Azure/GCP 帳戶、權限、配額、政策掛載與傳播；契約模擬不能保證實際入口已受保護。
- 未測試：HTTP.sys TLS 綁定、正式憑證與可信反向代理的完整流程、GUI 實際操作、安裝程式端到端安裝。
- 未測試：跨主機長時間斷線、程序強制終止時的全面故障注入、正式負載下的工作集與 I/O。Azure 產品規則仍要求單一控制器管理；切換雲端提供者前須處理舊規則與待送工作。
- 目前不能宣稱所有功能與環境絕無錯誤。以上剩餘驗收需要指定測試主機、TLS／代理設定與雲端測試資源；尚未對正式雲端或系統防火牆進行這些操作。

重新核對的主要官方依據

- [AWS UpdateIPSet](https://docs.aws.amazon.com/waf/latest/APIReference/API_UpdateIPSet.html)：完整集合與 LockToken。
- [Azure Security Rules](https://learn.microsoft.com/en-us/rest/api/virtualnetwork/security-rules/create-or-update?view=rest-virtualnetwork-2025-07-01)、[GCP securityPolicies.patch](https://docs.cloud.google.com/compute/docs/reference/rest/v1/securityPolicies/patch)：規則操作、狀態與 fingerprint。
- [OpenStack Networking](https://docs.openstack.org/neutron/latest/admin/intro-os-networking.html)：Security Group 的 allow 模型。
- [Windows Firewall](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)、[OWASP REST](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)：入口、傳輸及驗證要求。
- [.NET Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)、[協作取消](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)、[Timer.Dispose](https://learn.microsoft.com/en-us/dotnet/api/system.threading.timer.dispose?view=net-10.0)：有界工作、取消及停止生命週期。
- [正規表示式最佳實務](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-regex)：NonBacktracking、輸入與期限限制。
- [SQLite AUTOINCREMENT](https://www.sqlite.org/autoinc.html)：識別碼遞增但不保證連號；[Microsoft.Data.Sqlite 非同步](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)：不能將 async 包裝視為非同步磁碟 I/O。

本機驗證日誌位於目前使用者暫存目錄：idds-final-build.log、idds-all-green-tests.log、idds-final-shared-tests.log、idds-final-vulnerable.json、idds-final-outdated.json、idds-owner-audit.json。未刪除正式安裝包、原始碼或既有使用者檔案，未重設儲存庫 ACL。
