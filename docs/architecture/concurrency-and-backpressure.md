# 並行處理、背壓與 UI 執行緒架構

## 官方設計依據

- .NET bounded channels 與滿載模式：<https://learn.microsoft.com/dotnet/core/extensions/channels>
- WinForms 跨執行緒與 `Control.InvokeAsync`：<https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls>
- `Control.InvokeAsync` .NET 10 API：<https://learn.microsoft.com/dotnet/api/system.windows.forms.control.invokeasync?view=windowsdesktop-10.0>

## 防護事件資料流

所有 FileMaker、FTP、SMTP、POP3、MySQL、SQL Server、Terminal Server、Web Security、Windows Security、Kerberos、RRAS、AD 驗證與 Windows DNS Agent 都透過相同的 `AttackDetected` 邊界送入 `SecurityEventPipeline`。

該管線具備：

- 容量可設定的 `Channel<T>`，預設最多等待 4,096 筆事件。
- 多 producer、單 consumer，依接受順序執行資料庫、稽核與防火牆處理。
- 每筆已接受事件會先寫入 SQLite `ProtectionEventInbox`，再送入記憶體 channel；服務重啟時會依接收順序重播 pending、processing 與 failed 事件。
- `AllowSynchronousContinuations = false`，避免 consumer 工作回到 Event Log 或封包擷取 callback。
- Service producer 使用 `WriteAsync` 的同步事件橋接，在容量滿載時等待可用空間，將背壓傳回 Agent callback 而不增加無界工作；停止後的事件會明確拒絕、記錄錯誤並增加 rejected metric，不會靜默遺失。
- Service 停止時先停止並卸載 Agent，再關閉 writer，最多等待設定秒數 drain 已接受事件。
- 每筆 consumer 失敗相互隔離，不會終止後續防護事件；失敗事件保留於 durable inbox，供下次啟動重試。
- 停止服務時若 drain 超時，未完成事件仍保留於 durable inbox，不會因行程結束而消失。

`Protection:SecurityEventQueueCapacity` 允許 16 至 1,048,576，預設 4,096。`Protection:SecurityEventDrainTimeoutSeconds` 允許 1 至 300 秒，預設 30 秒。`Protection:SecurityEventRecoveryBatchSize` 允許 1 至 1,000,000，預設每次啟動重播 10,000 筆。容量必須以壓力測試、尖峰偵測率、平均防火牆延遲與可用記憶體決定，不能只增加到任意大值。

事件處理採 at-least-once 語意；防火牆效果由資料庫 lock 狀態與啟動時 reconciliation 收斂。Lock 先保存為 requested，只有 Windows Firewall 確認套用後才轉為最終 soft/hard 狀態；啟動時會補套缺少的規則、完成 requested 狀態，並移除已不在有效 lock 集合中的 Cyberarms 規則。

Metrics meter `Cyberarms.SecurityEvents` 提供：

- `cyberarms.security_events.accepted`
- `cyberarms.security_events.processed`
- `cyberarms.security_events.rejected`
- `cyberarms.security_events.failures`
- `cyberarms.security_events.queued`
- `cyberarms.security_events.recovered`
- `cyberarms.security_events.queue_delay`（毫秒）
- `cyberarms.security_events.processing_duration`（毫秒）
- `cyberarms.security_events.recovery_age`（秒）
- `cyberarms.security_events.drain_timeouts`

Runtime health check 會回報 `unfinished_security_events`；未完成數超過設定的記憶體佇列容量時回傳 degraded，供 Windows 服務監控或外部 health-check host 告警。

## SQLite 並行與交易

同步與非同步的一般查詢各自使用短生命週期、具 pooling 的獨立連線，避免多執行緒共用同一 `SqliteConnection`。需要原子性的設定、Agent 與 lock 寫入則由 `ExecuteInTransaction` 明確擁有連線與 transaction，所有 transaction 內 command 都使用同一連線。測試涵蓋並行讀寫以及例外時 rollback。

## Raw Socket

封包擷取使用獨立 bounded channel 與單一 reader。容量滿時 `TryWrite` 立即拒絕新封包，並由 received、dispatched、dropped 與 subscriber-failure 計數呈現負載。Agent 偵測結果再進入上層 `SecurityEventPipeline`，避免封包 consumer 執行防火牆慢速操作。

FTP、SMTP、POP3 與 Terminal Server 的舊式啟動 `Thread` 已移除。Sniffer 會在 Agent 生命週期內同步完成註冊，再由共用 `RawSocketReceiver` 的可取消非同步 receive loop 擷取封包，消除 Stop 與背景初始化同時修改 Sniffer 清單的競爭。

## Agent 生命週期

`Start`、`Pause`、`Continue` 與 `Stop` 由同一生命週期鎖序列化。AttackDetected 會逐一呼叫訂閱者並隔離個別例外，單一錯誤訂閱者不會阻止 Service 接收事件。

## WinForms UI

Admin 的 intrusion log、locks、dashboard statistics 與 Service 狀態查詢會在背景工作取得不可變 snapshot，再透過 .NET 10 `Control.InvokeAsync` 封送至 UI thread。手動解除封鎖的資料庫寫入，以及 Windows Service 的 Start、Stop 與 WaitForStatus 也在背景工作執行。UI callback 僅更新控制項，不執行資料庫或 ServiceController I/O。每次 timer refresh 以 `IsUpdating` 防止重疊，並在 `finally` 恢復狀態。

SMTP 測試仍由 UI `async` event handler 執行非同步 MailKit API；一般 `await` 會返回 WinForms synchronization context，控制項更新保留在 UI thread。

## 驗收要求

1. 以小容量測試 queue saturation，確認同步 producer 受到背壓、非同步 producer 可等待空間，停止後 rejected 指標及錯誤記錄增加。
2. 讓第一筆 consumer 工作失敗，確認第二筆仍處理。
3. Stop Agent producer 後關閉 pipeline，確認已接受事件按序 drain。
4. 以多執行緒同時 Start 同一 Agent，確認只啟動一次。
5. 在 UI refresh 執行慢速資料庫查詢時拖曳視窗，確認 message loop 保持回應。
6. 在 `CheckForIllegalCrossThreadCalls` 開啟的 Debug 執行環境操作所有畫面，確認沒有跨執行緒控制項存取。
7. 在事件 processing 中強制終止服務，重新啟動後確認 durable inbox 重播，並由 firewall reconciliation 收斂 requested lock。
8. 製造資料庫與 Windows Firewall 規則差異，確認缺少規則會補建、多餘規則會移除，且失敗時保留 requested 狀態供下次重試。
