IDDS Community 實作、安全性與效能審查（2026-09-05）

已完成 main 分支 fast-forward Pull：ce0ec16 → e0ca8d2081795ca2066cbdf62d9a4a5892662ff7。Pull 前工作目錄乾淨。本報告保留更新後、修正前的審查證據。後續已依修正計畫修改產品實作；最新驗證結果請見同目錄的 2026-09-05-validation-results.zh-TW.md。未提交或推送。

審查重點為管理 API、自助解鎖、設定匯出、雲端封鎖、Threat Hub、事件管線、SQLite 與慢速攻擊偵測；另抽查金鑰保護、擴充元件路徑與發行驗證。這是風險導向的深入審查，並非逐行覆蓋所有 UI、代理程式與平台。P1 表示應優先修正的重要安全或功能缺陷；P2 表示應排程修正的正確性與可靠性問題。未連接真實雲端帳戶，未修改本機防火牆或正式資料庫。

**驗證結果與限制**

- 非提升權限執行 .NET SDK 10.0.400，建置使用單一 MSBuild 節點、停用平行還原與節點重用。
- `dotnet build IDDSCommunity.slnx -m:1 -p:RestoreDisableParallel=true -nodeReuse:false -p:UseSharedCompilation=false` 在 NuGet 還原階段遇到 NU1301：TLS／認證失敗，以及 NU1900／NU1905 弱點資料取得失敗，已中止重複還原。不能確認完整建置零警告零錯誤，也不能宣稱套件沒有已知弱點或全為最新穩定版。
- `dotnet test IDDSCommunity.slnx --no-restore ...` 沒有產生測試執行結果；對 Shared.Test 追加一般詳細度後也僅出現空建置成功，未出現任何測試數量。套件尚未成功還原，這不算測試通過。
- 隔離的 net10.0 驗證程式直接編譯目前儲存庫的 ActionTokenService、SlowAndLowAttackDetector 與四個雲端提供者原始檔，不使用第三方套件。HTTP 完全由記憶體模擬處理常式攔截，未送到外部服務。
- 重現：IPv6 權杖自身驗證為 false；容量 10 實際保留 25 個 IP；1,000 個相異帳號估算為 22；兩個 Azure IP 都用 priority 110；兩個 GCP 解鎖 IP 產生完全相同的 removeRule?priority=1000 請求。
- 本次新產生的暫存 bin／obj 及可辨識的儲存庫 obj 擁有者為執行驗證的 CodexSandboxOnline 帳戶，未使用系統管理員擁有者；其與桌面登入的 User 帳戶不同。未對儲存庫 ACL 或既有檔案接管所有權。完成後未觀察到 dotnet／MSBuild／VBCSCompiler 殘留程序。

**優先修正發現**

1. **[P1] 不含機密的設定匯出仍洩露新增金鑰。** [ConfigurationTransferService.cs:51](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/ConfigurationTransferService.cs:51) 將整張 AppConfig 複製至 ApplicationSettings，只有 SMTP 密碼進入加密 Secrets。IddsConfig 的 ManagementApiKey、ThreatHubApiKey、CloudPerimeterApiKey、SelfServiceTotpSecret 等都直接存於 AppConfig，因此 Export(false) 也會產出這些明文值；Export(true) 不會替這些欄位加密。設定檔分享、備份或支援附件會擴大憑證暴露範圍。應用明確可匯出欄位清單，將所有機密集中至加密區段並升版格式，加入「未包含機密時任何金鑰均不得存在」的回歸測試。此結論來自資料流靜態追蹤，並未讀取使用者實際金鑰。[OWASP 機密管理](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)支持對機密的完整生命週期保護。

2. **[P1] 管理 API 空金鑰會跳過全部驗證，且監聽外部明文 HTTP。** [ManagementApiHttpServer.cs:136](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/ManagementApi/ManagementApiHttpServer.cs:136) 僅在金鑰非空白時驗證。功能預設關閉，但 UI 可啟用並保留空金鑰；Start 使用 http://+，自動防火牆管理又建立傳入允許規則。啟用後，能連到端點的人可查閱與修改鎖定資料；設定非空金鑰也仍會以 HTTP 傳送。應在服務啟動及設定匯入時拒絕無有效憑證的配置，使用 TLS，限制管理監聽位址與來源。[OWASP REST 安全](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)要求敏感 API 的傳輸及存取控制。

3. **[P1] API 封鎖／解鎖回報成功，但未套用防火牆。** [ManagementApiHttpServer.cs:205](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/ManagementApi/ManagementApiHttpServer.cs:205) 直接建立 HARDLOCK；[Locks.cs:635](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/Locks.cs:635) 直接將鎖定改為 UNLOCKED。兩者都只寫資料庫。每秒清理的 GetUnlockList 只挑到期鎖與 UNLOCK_REQUESTED，不會撿起已標 UNLOCKED 的項目；完整 ReconcileFirewallState 僅在啟動呼叫。結果是新增鎖未即時阻擋、解除鎖仍被阻擋，通常要等服務重新校準。ChatOps 與自助解鎖共用這個錯誤。應交由共用處置服務寫入要求狀態、套用防火牆、最後回寫成功／失敗；回傳接受要求與完成處置要分開。需測試假防火牆確實收到操作，以及 COM 失敗可重試。

4. **[P1] HiCloud 的「Block」實際新增 OpenStack 允許規則。** [ChunghwaHiCloudPerimeterProvider.cs:63](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/CloudPerimeter/Providers/ChunghwaHiCloudPerimeterProvider.cs:63) POST ingress security_group_rule，remote_ip_prefix 為攻擊來源，未指定協定／連接埠。依它使用的標準 Neutron API，這會允許該來源，而非拒絕；描述文字含 Block 不會改變語意。隔離驗證已捕獲這份 payload。應先停止將此介面當作阻擋能力，改接支援 deny 的防火牆介面並驗證政策結果。未對 HiCloud 的供應商客製行為進行實測；標準語意由 [OpenStack 最新 Networking 文件](https://docs.openstack.org/neutron/latest/admin/intro-os-networking.html)確認。

5. **[P1] GCP 解鎖忽略目標 IP，可能刪除其他規則。** [GcpCloudArmorPerimeterProvider.cs:98](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/CloudPerimeter/Providers/GcpCloudArmorPerimeterProvider.cs:98) 對所有 IP 都刪 priority=1000，Block 也固定此值。不同 IP 無法各自管理，既有 priority=1000 的非本產品規則也可能被刪除。隔離驗證已確認不同輸入產生同一 URL。應保存 IP 與規則識別／優先順序的映射，刪除前檢查規則歸屬及來源範圍，追蹤非同步 operation 最終結果。[Google 官方 API](https://docs.cloud.google.com/compute/docs/reference/rest/v1/securityPolicies)明確以 priority 識別刪除目標。

6. **[P1] AWS 原生 WAFv2 請求契約不成立。** [AwsPerimeterProvider.cs:66](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/CloudPerimeter/Providers/AwsPerimeterProvider.cs:66) 對預設原生端點發出 Action／IpCidr／IpSetId 格式，而非必要的 Addresses、Id、Name、Scope、LockToken；也沒有讀取既有集合與樂觀鎖流程。即使認證成功，payload 仍不符合 UpdateIPSet。應使用官方 SDK／標準 AWS 簽署與認證，先讀集合再修改完整集合，遇到版本衝突重新讀取，避免覆寫其他 IP。若是客製閘道協定，應作為獨立 Generic provider。[AWS UpdateIPSet](https://docs.aws.amazon.com/waf/latest/APIReference/API_UpdateIPSet.html)確認必要欄位及完整取代語意。

7. **[P1] Azure 第二個阻擋規則會與第一個優先順序衝突。** [AzureNsgPerimeterProvider.cs:77](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/CloudPerimeter/Providers/AzureNsgPerimeterProvider.cs:77) 依 IP 命名不同 inbound 規則，卻全部使用 110。隔離驗證確認兩個 IP 都送 110。應集中管理多來源前綴或安全分配未使用的優先順序，保存映射並考量 NSG 配額，不可只取 IP 雜湊而不處理碰撞。[Microsoft NSG 文件](https://learn.microsoft.com/en-us/azure/virtual-network/manage-network-security-group)確認優先順序唯一性要求。

8. **[P1] 自助門戶的 allow 規則不能穿透自身的 IP block。** [ProtectionService.cs:889](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/ProtectionService.cs:889) 增加門戶 port allow；FirewallPolicyManager 的 IP block 使用所有協定。當封鎖規則已套用時，受封鎖的來源不能直接連入同機門戶，因而無法取得 TOTP 表單。Windows 明確 block 優先於 allow，較細的 port allow 也不能覆蓋。應設計獨立可達的驗證入口或重新設計阻擋範圍，並在隔離 Windows VM 驗證真實封包；若使用代理，還須建立可信的原始來源辨識，目前伺服器只讀 RemoteEndPoint。[Windows 防火牆規則優先順序](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)。

9. **[P1] 慢速攻擊偵測容量失效，且超限後每事件掃全表。** [SlowAndLowAttackDetector.cs:52](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/Correlation/SlowAndLowAttackDetector.cs:52) 超過容量只刪除三倍半衰期以前的項目，近期新 IP 全數保留；不是註解所稱 LRU。預設情況可持續累積約 72 小時近期來源，每次新事件又掃全部字典，形成記憶體成長及近似二次累積成本。隔離驗證：maxCapacity=10，25 個近期來源全部留存。應硬性限制容量，使用有界淘汰／分片與獨立定時清理，明定淘汰對安全判斷的影響。需在持續全新來源與並行寫入下驗證容量及延遲。

10. **[P2] HyperLogLog 的 leading-zero 計算永遠為零。** [SlowAndLowAttackDetector.cs:75](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/Correlation/SlowAndLowAttackDetector.cs:75) 將最高位元 OR 成 1 後再 LeadingZeroCount，所以暫存器最多為 1，無法正確估計帳號多樣性。隔離驗證：1,000 個不同帳號估成 22。應針對排除索引位元後的有效 hash 寬度正確計算 rank，驗證小樣本校正與估算誤差；帳號暫存器還需要與時間窗相容的輪替，避免分數衰減但歷史多樣性永不過期。[BitOperations 官方定義](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.bitoperations.leadingzerocount?view=net-10.0)支持前導零判定；數值結果來自本機重現。

11. **[P2] 動作權杖不支援 IPv6，且預設簽署密鑰可預測。** [ActionTokenService.cs:47](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/Security/ActionTokenService.cs:47) 用冒號分隔全部內容且要求恰好四段，IPv6 本身也含冒號。直接生成 2001:db8::1 的權杖後驗證失敗。另外預設 secret 是固定文字加 MachineName 的 SHA-256，機器名稱不是機密。應使用具版本的明確編碼與隨機保存密鑰，移除可預測備援；處置不宜用 GET，需避免連結預覽／預取意外執行。若保留一次性動作，需原子記錄已使用 nonce。[OWASP REST 安全](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)提供方法與權杖處理依據。

12. **[P2] Threat Hub 不清除過期情報，每次回傳全部歷史資料。** [ThreatIntelligenceHubServer.cs:248](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/ThreatIntelligenceHubServer.cs:248) 對 activeThreats 全量複製，未依 LastSyncUtc 篩選，也沒有 ExpiresUtc 淘汰；registeredNodes 也沒有容量／離線到期清理。HandleClusterThreatReceived 未拒絕過期項目，可能在清理解除後下一輪再次封鎖、下一秒又解除，增加日誌與防火牆 I/O。Hub 重啟則記憶體清空。應持久化且清除到期情報、入口驗證 TTL／來源，採有界分頁與穩定游標；需要撤銷記錄，避免單靠 timestamp 漏掉更新。此項依目前完整資料流確認，尚未做多節點壓測。

13. **[P2] HTTP 請求與背景通知缺少整體容量及停機管理。** [ManagementApiHttpServer.cs:109](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/ManagementApi/ManagementApiHttpServer.cs:109) 每請求新增未追蹤工作，POST ReadToEndAsync 沒有應用層 body 上限或取消期限；SelfService 與 Hub 也有相同模式。ProtectionService 又不等待通知／雲端工作的結果。HttpListener 本身有底層限制，但不能取代應用層的總並行、JSON 大小與處置佇列上限。應使用有界佇列、有限 worker、讀取大小與期限限制、停機 drain；安全處置保存 outbox，通知可聚合。不能只在已建立無限 Task 的內部加 SemaphoreSlim，否則等待 Task 仍無界。[.NET Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)提供有界背壓機制。

14. **[P2] SOAR 大量輸出會堵塞管線並被當成逾時。** [SoarRemediationExecutor.cs:76](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Service/Notifications/SoarRemediationExecutor.cs:76) 重新導向 stdout 與 stderr，但從不讀取，只同步 WaitForExit(15000)。輸出超過管線容量的正常腳本會卡住，15 秒後被殺；每個操作還占用 ThreadPool 執行緒，啟動後取消權杖不會中止等待。應同時持續消耗兩個串流（限制保留量）、WaitForExitAsync 搭配期限，逾時終止整個子程序樹並觀察結果。引數應以 ArgumentList 傳遞並對腳本類型建立明確契約，避免拼接引號破壞引數邊界。[Microsoft RedirectStandardOutput 文件](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput?view=net-10.0)說明此管線互等問題。

15. **[P2] 假釋判定使用建立時間，無法證明指定期間沒有攻擊。** [Locks.cs:526](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/Locks.cs:526) 僅以 HARDLOCK 與 LockDate 判定，未檢查永久期限及最後攻擊時間；ProtectionService 直接 SetProbation 並移除本機防火牆，沒有呼叫雲端解鎖。這不符合規範的「永久鎖、連續 90 天無活動」，可能提前放行本機或留下雲端封鎖。應保存可信的最後活動／封鎖期限，於同一處置狀態機內執行假釋及各邊界校準。若封包已被防火牆丟棄而不可觀測，也不能將無應用程式事件直接解讀為無攻擊。

**效能改善建議與量測方式**

下列是程式碼成本分析與待驗證設計，未量測端到端改善百分比。優先先修復上述正確性問題，再建立基準。

| 範圍 | 現有成本與依據 | 建議 | 驗證指標 |
| --- | --- | --- | --- |
| SQLite 寫入 | Database.cs 的 Async 包裝仍呼叫 Microsoft.Data.Sqlite；SecurityEventPipeline.TryPublish 在寫入 Channel 前先同步持久化 inbox，滿載還 INSERT 後 DELETE | 保留既有 WAL；使用有限寫入工作者與小批次交易／重用 command，先定義持久化成功的回覆語意；不要以增加 Task.Run 數量擴張寫者 | 每秒交易／事件、busy 重試、callback p99、磁碟寫入、斷電重播正確性 |
| GeoIP 查詢 | [GeoIpLookupService.cs:251](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/ThreatIntelligence/GeoIpLookupService.cs:251) 每次線性走訪全部範圍；國家集合每次重建 | 更新時建置不可變區間索引或前綴樹，快照交換；保留重疊區間的既定優先語意；設定改變時才重建國家 HashSet | 10 萬／100 萬範圍的命中與未命中 p95、配置量、並行更新一致性 |
| Bogon 檢查 | [BogonIpFilter.cs:91](D:/Dev/Project/Application/IDDSCommunity/src/IDDSCommunity.IntrusionDetection.Shared/ThreatIntelligence/BogonIpFilter.cs:91) 每 IP 線性掃 Fullbogons，匯入 K 筆情報與 B 筆前綴會有 O(K×B) 成本 | 依位址族建前綴樹／前綴長度索引，批次使用同一快照；消除重複解析與 GetAddressBytes 配置 | 匯入時間、每 IP 比對數、配置量、IPv4-mapped IPv6 一致性 |
| Windows 防火牆 | FirewallPolicyManager.cs:385 每次新增都讀取、合併、重寫 RemoteAddresses 字串；一次匯入大量 IP 重複全清單工作 | 有限批次更新並確認 COM 執行緒與序列化契約；必要時依實測大小分組規則 | COM 呼叫次數、全量 1 萬 IP 更新時間、失敗後重試與封包驗證 |
| 多節點聯防 | 每節點每輪完整複製及 JSON 序列化 N 筆情報，總傳輸近似節點數×N；離線 Edge queue 無界 | 游標增量、分頁、持久 outbox、去重、TTL、指數退避及排程抖動；依 IP 保持 block/unblock 順序 | 傳輸位元組、配置／LOH、斷線 24 小時 backlog、收斂延遲 |
| 情報下載 | ExternalThreatFeedSubscriberService.cs:201 先完整緩衝 response，再轉字串及 List；筆數上限不限制下載位元組 | ResponseHeadersRead、總位元組限制、串流解析、適度並行下載、有限批次入庫，下載取消與停機連動 | 最大記憶體、超大／慢速 response、匯入吞吐、取消延遲 |
| 通知／SOAR | 警報增加時出站 Task 與程序未受整體上限控制，雲端 bool 失敗常被忽略 | 有界 dispatcher、每通道速率限制／聚合、持久處置 outbox、串流非同步等待、失敗狀態與重試 | 同時連線／程序數、ThreadPool queue、通知延遲、停機遺漏數 |

[Microsoft.Data.Sqlite 官方文件](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)確認其 ADO.NET 非同步方法仍同步執行；[SQLite WAL 文件](https://www.sqlite.org/wal.html)確認讀寫可並行但同時仍只有一個 writer。因此資料庫改善重點是降低交易與鎖競爭，不能把 async 方法名稱當成非同步磁碟 I/O。現有 Database.cs 已設 WAL、私有快取及連線池，應保留並量測，不是再次加入相同設定。

**既有測試的遺漏與後續驗收**

[CloudPerimeterAdaptersTest.cs:18](D:/Dev/Project/Application/IDDSCommunity/tests/IDDSCommunity.IntrusionDetection.Shared.Test/CloudPerimeterAdaptersTest.cs:18) 的 mock 預設對任意請求回 200，主要檢查字串是否包含 IP／資源 ID，所以能把錯誤 AWS payload 與相反的 OpenStack 語意當成功。應用官方契約驗證 request，模擬兩個 IP、既有非產品規則、409／429、非同步 operation 失敗、重新啟動後解鎖，不應只測第一筆 happy path。

修正順序建議：先處理設定洩密與未驗證管理介面、暫停錯誤雲端阻擋；接著統一封鎖狀態機與自助入口；再修正有界容量、TTL、HLL／IPv6；最後進行資料庫、索引、COM 與出站工作的基準測試。

必要驗收包括：空金鑰服務拒絕啟動、設定匯出無明文機密、API 操作真實改變防火牆、雲端兩個 IP 各自獨立、GCP 不刪除非產品規則、過期情報不再封鎖、滿載記憶體有界、SOAR 大輸出不阻塞、停機不遺失已接受處置。恢復 NuGet 存取後，還必須執行完整建置、MSTest 與直接／遞移套件弱點及版本掃描；目前未將任何空測試執行視為通過。

抽查中可確認已有 DPAPI 與隨機資料庫金鑰、設定 Secrets 的 Argon2id／AES-GCM 與 AAD 參數限制、擴充元件路徑與 reparse point 檢查、signed tag 驗證後傳遞確切 SHA 等防護。不過這不代表相關模組已通過完整安全認證，尤其新增 AppConfig 機密沒有接入既有匯出加密流程。
