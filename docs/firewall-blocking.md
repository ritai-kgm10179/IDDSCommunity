# 主機封鎖策略

IDDS Community 使用 Windows Defender 防火牆依遠端 IP 位址封鎖流量，原生涵蓋 IPv4、IPv6 與所有 IP 協定。

管理者可選擇：

- **僅封鎖入站（建議）**：阻止指定遠端位址連入本機，保留一般伺服器最容易理解與管理的行為。
- **雙向封鎖**：除入站外，也阻止本機對指定遠端位址送出流量。適用於需要抑制回應或主動連線的環境。

## 規則對帳與移除語意

服務一次擷取 IDDS Community 擁有的規則快照，再以正規化後的 CIDR 集合完成比對。僅入站模式以入站規則為有效集合；雙向模式以入站與出站交集為有效封鎖，並以兩個方向的聯集找出需要清除的孤立規則。只有屬性確實不同時才寫入 Windows Firewall COM，避免每次啟動反覆提交未變更的規則。

少量單一位址不會自動擴大成 `/24` 或其他較大的網段。從既有 CIDR 移除單一 IP 或安全網段時，系統會做精確的 CIDR subtraction，保留未重疊的剩餘範圍。資料庫鎖定先記為 requested，只有防火牆成功套用後才確認為 soft 或 hard；失敗項目保留 requested 狀態供下次對帳重試。

## 百萬級高容量擴展與動態分片 (Million-Scale Sharding)

為支援十萬至百萬級別的大規模攻擊來源阻擋，系統建構了動態分片與增量調和機制：

1. **單一規則位址上限約束 (1,000 筆)**：
   Windows Defender 防火牆底層 COM 介面在單一規則內包含龐大逗號分隔 IP 字串時，會出現嚴重的序列化與驗證延遲。系統嚴格將單一防火牆規則的遠端位址上限約束在 **1,000 筆** 以內，杜絕 COM 逾時與系統負擔。
2. **256 個穩定分桶 (Stable Buckets) 與序列分片**：
   - 採用 32 位元 FNV-1a 雜湊算法將 IP 均勻映射至 `0..255` 號穩定分桶。
   - 規則命名採用穩定格式：`IDDSCommunity_BlockAttacker_AllPorts_b{bucket:D3}_s{sequence:D4}`（例如 `IDDSCommunity_BlockAttacker_AllPorts_b042_s0000`）。
   - 當單一分桶超過 1,000 筆時自動遞增序號並分裂新 Shard，使百萬級 IP 能線性平滑擴展。
3. **$\Delta$ 穩態調和與零 COM 寫入**：
   - 在一致狀態下（既有規則與資料庫期望狀態吻合），服務啟動與定時檢查對 Windows 防火牆的 COM 寫入與刪除操作嚴格為 **0 次**。
   - 穩態調和開銷僅取決於異動量 $\Delta$，而非整體資料庫總量 $N$。
4. **孤兒規則與舊版批次規則自動清理**：
   - 調和計劃會比對 Windows 防火牆上所有託管規則，凡不在當前期望目標清單中的孤兒規則（含歷史舊版未分片或簡易序號規則 `BlockAttacker_0` 等，以及 IP 已完全清空的空 Shard），均會自動排入刪除清單進行精準清理，避免廢棄規則殘留。
5. **持久化異動日誌與分散式租約**：
   - 由 `FirewallStateStore`（SQLite `firewall_desired_addresses`、`firewall_change_journal`、`firewall_applied_shards`）負責管理異動日誌，具備原子租約領取機制，確保重啟與並行時的冪等性。


## 為何不提供黑洞路由

Windows `CreateIpForwardEntry2` 建立的是目的地路由。將攻擊來源 IP 加入路由表只會改變本機送往該目的地的出站路徑，並不會依封包來源位址攔截入站流量，因此不能取代防火牆，也不應宣稱為 DDoS 防護。

Windows Vista 與 Windows Server 2008 之後，IPv4 與 IPv6 預設採 Strong Host 模型。IDDS Community 不會為封鎖功能啟用 Weak Host；該設定與來源 IP 封鎖無關，並會改變跨介面收送封包的安全邊界。

若未來加入新的封鎖後端，必須能直接依遠端來源位址處理 IPv4 與 IPv6 入站流量，具備可還原的生命週期、狀態對帳、權限失敗記錄及整合測試，且不得以目的地路由模擬入站封鎖。
