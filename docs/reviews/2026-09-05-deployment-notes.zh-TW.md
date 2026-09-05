IDDS Community 2026-09-05 修正部署與相容性說明

本文件對應目前工作區修正，驗證狀態請參閱同目錄的實作計畫。尚未執行真實雲端、正式 TLS 綁定、Windows 服務安裝或管理主控台互動驗收。

設定套件

- 新匯出格式為 schema 2。一般匯出僅含明確允許的公開 AppConfig；所有代理自訂設定以及其餘 AppConfig 均須選擇包含機密資料，透過原有 Argon2id/AES-GCM 加密。
- 不包含機密的匯入會保留目的端的機密與代理自訂設定。要移植這些設定，請使用含機密匯出及至少 12 字元的密碼短語。
- schema 1 的 SMTP 加密資料仍使用原版 AAD 驗證。含明文應用程式或代理機密的舊套件會拒絕匯入，請在可信來源端重新匯出。新版套件不能交給舊版程式匯入。
- 匯入前的備份仍由加密 SQLite 備份流程建立，設定套件驗證失敗不得套用部分資料。

HTTPS 與自助入口

- Management API、Threat Hub 及自助門戶改用 HTTPS。啟用前必須為設定的通訊埠完成 HTTP.sys URL ACL 與有效憑證 SSL 綁定；憑證名稱必須符合用戶端使用的主機名稱。程式不會自動產生、信任或安裝憑證。
- Management API 與 Threat Hub 必須有非空白金鑰。Management API 使用 X-Api-Key 或 Authorization: Bearer；Hub 使用 X-IDDS-ThreatHub-ApiKey。未授權請求回應 401。管理 API 健康端點同樣需要驗證。
- 自助門戶需要獨立可信反向代理與 TOTP 密鑰。代理來源 CIDR 使用既有 TrustedProxyCidrs 設定，須只涵蓋真正受控的代理。代理應清除外部傳入的 Forwarded/X-Forwarded-For，再寫入經驗證的真實來源鏈。
- Windows 明確封鎖規則優先於允許規則，因此在同一部已封鎖來源的主機增開門戶通訊埠，無法保證被封鎖的來源能直連門戶。獨立代理必須能從不同來源連到服務主機；禁止將整個使用者網路設為可信代理。
- POST /api/v1/locks、DELETE /api/v1/locks/{IP} 及自助解鎖回應 202 代表要求已接受，需查詢後續狀態。自助入口只允許軟封鎖，資料庫會再次拒絕同時具有硬封鎖的來源。
- ChatOps 動作端點必須使用 POST，保留管理 API 驗證與動作權杖驗證；GET 不改變狀態。新版權杖支援 IPv6，明確要求簽署密鑰，不支援舊版機器名稱預設密鑰。

情資同步與持久化

- Hub/Edge 必須一併升級。同步新增 Generation、Cursor、NextCursor、HasMore；每頁最多 256 筆。每個中繼端點維護獨立的收送游標，初次連線及資料庫世代變更時從零同步。
- Hub/Edge 情資保存在現有加密資料庫，最多 100,000 個不同來源；到期資料不回傳。交換期限按 ThreatFeedTtlDays 限制於 1–365 天，避免永久或已過期資料反覆造成重封鎖。
- 新 schema migration 15 建立情資、雲端待送匣及最後攻擊活動資料。既有硬封鎖的最後活動若無可靠歷史，採升級當下作為保守起點，避免日誌清理後立即誤判長期無活動而假釋。
- 雲端待送匣最多 100,000 個不同 IP，同一 IP 保留最新要求，單一工作者依序執行並對失敗退避。排入成功不等於雲端已生效；故障與停機後仍保留未完成要求。切換雲端提供者前應先處理舊提供者的規則與待送工作。

雲端提供者

| 提供者 | 新設定／操作要求 |
| --- | --- |
| AWS WAFv2 | ResourceId 使用 IPSet 完整 ARN，或搭配 TertiaryId 指定 IPSet 名稱；SecondaryId 為區域。ApiKey 欄位現在表示 AWS 認證設定檔名稱，留空使用服務帳戶的官方預設認證鏈。不得再輸入舊版 Bearer Token。IPSet 的 IPv4/IPv6 類型必須符合來源；完整集合更新會保留其他來源並處理 LockToken 衝突。 |
| Azure NSG | ResourceId 為 NSG 名稱，SecondaryId 為 Subscription ID，TertiaryId 為 Resource Group；使用有效 ARM Bearer Token。產品規則必須由單一控制器管理。舊版無歸屬標記的規則不自動刪除，需管理員辨識後遷移。 |
| GCP Cloud Armor | ResourceId 為全域 Security Policy 名稱，SecondaryId 為 Project ID，ApiKey 為有效 OAuth Bearer Token。使用 fingerprint PATCH 保留完整現有規則，遇 412 重新讀取後重試；政策必須具有足夠規則配額。 |
| HiCloud / Neutron | 標準 Security Group 是 allow 模型，無法表示封鎖；此提供者明確回報不支援，不再建立可能放行攻擊者的規則。可改用有明確 deny 契約的 Generic Webhook。 |

Azure/GCP 會檢查較高優先順序規則；無法保證阻擋的配置不回報封鎖成功。即使遠端 API 作業成功，仍須在實際流量入口驗證政策掛載、權限、配額與傳播延遲。未提供真實雲端環境，本次契約模擬不能取代部署驗收。

資源限制與 SOAR

- 每個新增有界 HTTP dispatcher 使用 4 個工作者及 32 個等候請求，滿載回 503；管理／門戶本文上限 64 KiB、Hub 本文上限 1 MiB。本文讀取及處理設有期限。
- 通知使用 128 筆佇列及 4 個工作者；滿載拒絕會寫入診斷記錄，通知不等同於持久化安全事件。SOAR 15 秒期限，並持續讀取 stdout/stderr，取消時終止子程序樹。
- PowerShell 使用 ArgumentList 傳遞具名引數；CMD/BAT 改以 IDDS_IP_ADDRESS、IDDS_LOCK_TYPE、IDDS_AGENT、IDDS_DETAILS 環境變數取得事件資料，舊批次檔若讀取 %1 等位置引數必須調整。
- 外部情資下載上限 8 MiB、GeoIP 單一檔案上限 64 MiB，超限時拒絕替換目前快照。必要時先確認供應者檔案格式與大小，不應直接移除上限。

官方依據：[Windows 防火牆規則優先順序](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)、[AWS UpdateIPSet](https://docs.aws.amazon.com/waf/latest/APIReference/API_UpdateIPSet.html)、[Azure Security Rules](https://learn.microsoft.com/en-us/rest/api/virtualnetwork/security-rules/create-or-update?view=rest-virtualnetwork-2025-07-01)、[GCP securityPolicies.patch](https://docs.cloud.google.com/compute/docs/reference/rest/v1/securityPolicies/patch)、[OpenStack Security Groups](https://docs.openstack.org/neutron/latest/admin/intro-os-networking.html)。
WAF 檢查限制

- 輸入超過 65,536 字元或任一規則超過 200 ms，會回報 Inspection.InputLimit 或 Inspection.Timeout，不再視為安全略過。合法超長資料同樣可能觸發限制，部署前應使用實際請求大小分布驗收。
- 正規表示式採用 NonBacktracking 線性時間引擎，保留原有攻擊分類；這不代表可偵測所有應用層攻擊。
- GeoIP 與 Fullbogons 更新要求已設定的來源均下載有效資料，任一失敗保留既有快照；停止更新服務會取消下載，停止後應建立新的服務執行個體再啟動。