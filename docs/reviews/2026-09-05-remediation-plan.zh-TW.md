IDDS Community 完整修正實作計畫

基準版本 e0ca8d2；對應 2026-09-05 審查報告 15 項發現。計畫以規格核對、程式碼測試及實際部署驗收分層驗證，不能宣稱絕無錯誤。禁止在未有證據時標記完成。

| 階段 | 發現 | 實作及相容性處理 | 驗收 |
| --- | --- | --- | --- |
| A | 9、10、11、14 | 有界 O(1) 淘汰、修正 HLL rank／時間窗；IPv6 權杖明確編碼與必要密鑰；SOAR 安全引數、非同步輸出及取消 | 並行容量、估算誤差、時間倒序、權杖竄改、空密鑰、大量雙串流輸出 |
| B | 1 | 設定格式升版；AppConfig 與代理設定機密加密，未匯出時保留目的端機密；支援舊版 AAD 解密但拒絕舊版明文機密 | 跨版本匯入、無機密匯出、錯密碼／參數竄改、原子回滾 |
| C | 2、3、8、13 | API 必要驗證、TLS、有限請求與大小；共用防火牆處置；門戶採明確可信代理入口、禁止錯誤同機 allow 假設；回應要求／完成狀態分離 | 空金鑰、未信任代理、IPv6、封鎖／解鎖失敗重試、停機排空、body 上限 |
| D | 4、5、6、7 | Neutron security group 不支援 deny，明確拒絕此錯誤能力；AWS 改官方請求與認證流程；Azure/GCP 規則歸屬、唯一識別與並行衝突；追蹤遠端操作狀態 | 兩個 IP、既有規則保護、非成功回應、逾時與重新啟動、契約模擬；真實雲端測試另列 |
| E | 12、15、13 | Hub TTL／有界同步及持久資料、來源過濾；永久封鎖最後活動與假釋狀態；雲端處置可重試與有序；停機管理 | TTL 不再回鎖、白名單/Bogon、斷線重試、假釋活動邊界、封鎖／解鎖順序 |
| F | 效能建議 | GeoIP／Bogon 快照索引、下載大小限制／串流、有限通知；SQLite／COM 批次必須保留持久化與順序語意，依基準決定批次值 | 工作集、p95/p99、配置量、最大佇列、吞吐與故障恢復 |
| G | 全部 | 完整方案建置、MSTest、套件弱點與版本掃描、擁有者／CRLF／BOM 稽核，更新文件及剩餘限制 | 0 警告／0 錯誤、有實際測試數量；不得將空執行當成功 |

2026-09-05 重新核對的官方依據：

- [AWS UpdateIPSet](https://docs.aws.amazon.com/waf/latest/APIReference/API_UpdateIPSet.html)：完整集合更新、LockToken 樂觀鎖及必要欄位，不能用單 IP Action 假協定。
- [OpenStack Networking](https://docs.openstack.org/neutron/latest/admin/intro-os-networking.html)：Security Group 只包含 allow，因此不能藉該 API 實作 deny，也不能猜測 HiCloud 私有介面。
- [Azure NSG](https://learn.microsoft.com/en-us/azure/virtual-network/manage-network-security-group)、[Google securityPolicies](https://docs.cloud.google.com/compute/docs/reference/rest/v1/securityPolicies)：規則識別與優先順序必須正確；不能固定刪除 priority=1000。
- [Windows Firewall](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)：明確 block 覆蓋 allow，門戶可達性必須從入口架構解決。
- [OWASP REST](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)：必要驗證、HTTPS、輸入與方法限制。
- [WaitForExitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexitasync?view=net-10.0)：取消等待不等於終止程序，必須明確停止子程序樹並讀取輸出。
- [Microsoft.Data.Sqlite 非同步限制](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)：增加 async 包裝不等於非同步 I/O，批次與背壓不能犧牲持久性。

執行紀錄（仍在進行，不代表全部完成）：

- 已實作 A/B 的主要修正，第一批 20 項測試通過。後續加入 IPv6 權杖、容量、GeoIP/Bogon 邊界，第二批 34 項通過，無略過。
- 已實作 HTTPS/必要金鑰、有界 HTTP dispatcher、要求/完成狀態分離、可信代理門戶。門戶的軟封鎖解除另加原子 SQL 防止硬封鎖競爭。
- 已改用 AWSSDK.WAFV2 4.0.101.7；52 個套件鎖定檔以正常使用者還原後同步。Azure/GCP 精準歸屬與多 IP 規則修正，GCP 改用 fingerprint PATCH，Azure 更新 2025-07-01 API；具狀態契約測試已新增，尚待完整執行。
- Hub/Edge 以加密資料庫儲存有限情資、持久序號、世代及 256 筆分頁；交換期限上限、來源過濾及回呼完成後更新游標。雲端待送匣使用單一工作者、版本比對與退避；通知有界 128 筆、4 位工作者。
- GeoIP 改為保留原始重疊優先順序的區間快照及二分搜尋；Bogon 按前綴長度分組，使查詢成本不再隨前綴數線性增加。下載先取 headers 並限制實際讀取長度。
- 前一輪完整 Service 測試：88 通過、0 失敗、5 略過（合計 93）；包含真實 SOAR 雙串流輸出與取消終止子程序。原生 socket、EventLog、防火牆、Windows Service 整合仍未測試。
- 尚須完成：待送匣故障/重啟/順序測試、假釋與防火牆競爭回歸、Hub 客戶端停機與增量測試、HTTP 大小與空金鑰測試、雲端契約與條件衝突測試、完整方案建置及測試、效能基準、套件弱點/版本檢查、產物擁有者稽核、部署及相容性文件。真實雲端憑證/政策與 HTTP.sys TLS 綁定尚無驗證環境。
- 雲端規則 API 的成功代表該資源的處置結果，不能推定政策已掛載至所有實際流量入口。Azure 必須由單一控制器管理產品規則，沒有文件支持的條件式寫入不可憑空宣稱具備跨控制器交易保證。
- 追加官方依據：[Google securityPolicies.patch](https://docs.cloud.google.com/compute/docs/reference/rest/v1/securityPolicies/patch)（fingerprint 與 412 衝突）、[Azure Security Rules 2025-07-01](https://learn.microsoft.com/en-us/rest/api/virtualnetwork/security-rules/create-or-update?view=rest-virtualnetwork-2025-07-01)、[.NET Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)、[SQLite trigger](https://www.sqlite.org/lang_createtrigger.html)。

2026-09-05 後續進度：

- 完整 Shared 測試 228 通過、0 失敗；包括兩個 IP 的 Azure/GCP 有狀態契約、設定機密來回、資料庫升級和查詢索引回歸。
- 雲端待送匣已通過失敗後重啟重送及舊工作完成不得刪除新要求的真實 SQLite 測試。
- 52 個專案的直接及間接套件弱點掃描沒有回報已知弱點。版本掃描發現 CsWin32 0.3.333、MSTest 4.4.0，已升級並重新還原；需以最終建置及測試確認相容性。
- 新增 GeoIP/Bogon 部分下載失敗保留完整快照、GeoIP 無效內容拒絕、下載停止取消及釋放等待。已補相關測試，完整重跑中。
- 增加最近攻擊排除假釋與軟解除拒絕硬封鎖要求的資料庫回歸測試。管理介面更新 AWS 認證與資源欄位、HiCloud 能力限制，測試連線後釋放提供者。
- 本機 Debug 合成效能測試：8192 前綴、2048 次查詢，其中 1024 次命中；線性參考 102.295 ms，GeoIP 索引 0.676 ms，Bogon 索引 0.616 ms。每次均核對結果，並非正式工作負載吞吐保證。Bogon 原有每次位址 byte[] 配置已改 stackalloc，待重測配置量。
- SQLite 批次與防火牆 COM 批次尚未引入：目前沒有能證明實際瓶頸與適當批次大小的正式負載數據，不能以同步 API 的 async 包裝宣稱 I/O 最佳化。
- 取消與停止依據：[.NET 協作取消](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)、[Timer.Dispose](https://learn.microsoft.com/en-us/dotnet/api/system.threading.timer.dispose?view=net-10.0)。停止 timer 不代表已排入的非同步工作已結束，必須傳递取消並追蹤更新作業。
追加發現 16：WAF 逾時略過造成安全檢查失敗時放行。

- 完整方案重跑曾使既有 SQL 注入案例回傳 false。原程式捕捉 RegexMatchTimeoutException 後視為安全略過，是需修正的失敗路徑，而非僅調整測試順序。
- 實作規劃：既有固定特徵均不需要反向參照或 lookaround，適用 RegexOptions.NonBacktracking；使用線性時間引擎、65,536 字元輸入上限和每規則 200 ms 期限。超限與逾時分別回報 Inspection.InputLimit、Inspection.Timeout，不回報安全。
- 契約保持 bool 判斷及既有命中分類；額外分類用於辨識無法完成的檢查，部署端應觀察其發生率及長合法請求的影響。
- 已實作並通過 WAF 5 項測試：既有攻擊、正常請求、60,000 字元的正常及攻擊內容、超限內容。
- 依據：[Microsoft 正規表示式最佳實務](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-regex)、[RegexOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regexoptions?view=net-10.0)。
- SQLite 識別碼測試另修正為遞增與資料存在，不假設刪除後連號；依據 [SQLite AUTOINCREMENT](https://www.sqlite.org/autoinc.html)。
交付狀態（以下取代前述各次執行中的待辦清單）：

| 項目 | 狀態與證據 |
| --- | --- |
| A：容量、HLL、權杖、SOAR | 主要實作與回歸完成；包括真實子程序大量輸出與取消 |
| B：設定機密 | 主要實作與加密來回、目的端保留測試完成 |
| C：API、處置狀態、自助入口 | 主要實作與本機 HTTP／資料庫測試完成；真實 TLS、可信代理與防火牆整合未測試 |
| D：雲端提供者 | 主要實作與 AWS/Azure/GCP 契約測試完成；GCP 412 重試通過；真實雲端未測試 |
| E：Hub、待送匣、假釋 | 持久化、容量與分頁、重啟重試、順序、近期活動與硬封鎖保護測試完成；跨主機流量及故障壓測未測試 |
| F：索引與有界資源 | GeoIP/Bogon 索引、有限請求／通知／下載已實作，合成基準確認；正式負載容量與 I/O 基準未測試 |
| G：建置、測試、套件與產物 | 最終數量與證據統一記錄於 2026-09-05-validation-results.zh-TW.md |
| 追加 16：WAF | 線性比對、長度及時間限制、檢查失敗不放行已實作並驗證 |

SQLite／COM 批次調整屬需以正式負載實測決策的效能候選，未將其包裝成已完成的效能改善。所有需真實雲端、系統防火牆與 GUI 操作的驗收均維持「未測試」，不得以模擬替代。