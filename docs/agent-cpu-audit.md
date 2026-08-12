# Agent 常駐 CPU 稽核與修正計畫

稽核日期：2026-08-12

## 官方判準

- Microsoft 的 `SIO_RCVALL` 文件指出，啟用後會把指定介面收到的所有 IP 封包交給 Raw Socket。因此，為每個 Agent 建立 Raw Socket 會重複接收相同流量。
- Microsoft Windows Filtering Platform（WFP）最佳實踐建議使用 Application Layer Enforcement（ALE），並明確指出逐封包層級的過濾較慢。
- WFP 的 ALE 狀態式過濾只需分類連線的第一個封包，可大幅降低分類次數；需要封包內容檢查時，Microsoft 建議使用 Stream／Datagram Data 層。
- SharpPcap 6.3.1 為 MIT 授權的受控程式庫，可使用主機既有的 Npcap／WinPcap。IDDS Community 不散布或自動安裝兩者，避免改變 Npcap 的授權與安裝台數責任。
- WinDivert 2.2.2 的官方驅動簽署憑證已過期，且官方 Issue #401 確認部分安全軟體會阻擋同一雜湊的驅動；因此不納入正式執行或發行路徑。
- .NET bounded `Channel<T>` 適合建立有界生產者／消費者管線；容量耗盡時必須採取明確的背壓或丟棄策略，避免無界記憶體成長。

## 全部 Agent 稽核結果

| Agent | 資料來源 | 結論與處置 |
| --- | --- | --- |
| FTP | Raw Socket | 需要修正；改用每張 IPv4 介面的共用擷取器、集中 TCP 解析及通訊埠路由。 |
| SMTP | Raw Socket | 需要修正；同 FTP。 |
| POP3 | Raw Socket、清理 Timer | 需要修正；加入共用擷取，移除封包回呼中的 `Thread.Sleep(100)`；清理 Timer 不做忙碌等待。 |
| IMAP | Raw Socket | 需要修正；加入共用擷取與已解析 TCP 事件。 |
| IIS Authentication | 每 2 秒增量讀取記錄檔 | 已有重入防護、檔案位移及截斷辨識；未發現忙碌迴圈，不需本次修改。 |
| PostgreSQL | 每 2 秒增量讀取記錄檔 | 同 IIS Authentication，不需本次修改。 |
| OpenSSH | Windows Event Log 加增量記錄檔輪詢 | 已有事件式來源及輪詢重入防護，不需本次修改。 |
| RADIUS | Windows Event Log | 事件驅動，不需本次修改。 |
| Windows Network Logon | Windows Event Log | 事件驅動，不需本次修改。 |
| Windows DNS | Windows Event Log | 事件驅動，不需本次修改。 |
| Terminal Server TLS/SSL | Windows Event Log | 事件驅動，不需本次修改。 |
| SQL Server | Windows Event Log | 事件驅動，不需本次修改。 |
| MySQL | Windows Event Log | 事件驅動，不需本次修改。 |
| FileMaker | Windows Event Log | 事件驅動，不需本次修改。 |
| Web Security | Windows Event Log | 事件驅動，不需本次修改。 |
| Windows Security Base | Windows Event Log | 事件驅動，不需本次修改。 |
| Kerberos | Windows Event Log | 事件驅動，不需本次修改。 |
| RRAS | Windows Event Log | 事件驅動，不需本次修改。 |
| AD Credential Validation | Windows Event Log | 事件驅動，不需本次修改。 |

## 分階段計畫

1. 本次完成：優先透過 SharpPcap 使用主機已安裝且可開啟對應網卡的 Npcap／WinPcap，套用 BPF 本機 IPv4、TCP 與 Agent 通訊埠 Filter。
2. 本次完成：維持 bounded Channel、丟包計數與錯誤隔離，避免高流量造成無界排隊。
3. 本次完成：讓四個內容檢查 Agent 直接使用已解析 TCP 事件，消除第二次標頭解析。
4. 本次完成：後端順序為既有 Npcap／WinPcap、共用 Raw Socket；Pcap 啟動或執行期間故障時自動切換。Raw Socket 在複製及排隊前先做無配置的 IPv4／TCP／通訊埠判斷。
5. 本次完成：正式安裝包不散布 Npcap、WinPcap 或 WinDivert 驅動程式，只包含 MIT 授權的 SharpPcap 受控組件。
6. 發行前環境驗證：分別在有 Npcap、WinPcap 與兩者皆無的 Windows 測試機，以相同流量回放比較 CPU、配置率、GC、接收／分派／丟棄封包數，並執行長時間 soak test。

## `master` 分支比較

`master` 看似不會出現相同尖峰，不是因為使用較佳的封包處理架構，而是每個 Sniffer 只配置並接收 128 bytes。大於 128 bytes 的封包會被截斷，而 IP 解析器仍依原始 `TotalLength` 嘗試複製資料，例外狀況隨後被吞掉或寫入暫存記錄檔。這會降低後續實際解析量，但同時遺失或破壞正常的大型 TCP 封包內容，不能作為正確修正。

目前實作保留完整封包，改採以下方式避免 `master` 的資料遺失與 `main` 舊實作的 CPU 尖峰：

- 已安裝 Npcap／WinPcap 時優先使用 BPF Filter，排除無關 IP、非 TCP 及非 Agent 通訊埠流量；Filter 會隨 Agent 訂閱增減更新。
- 使用 `ReadOnlySpan<byte>` 與 `BinaryPrimitives` 驗證網路位元組序標頭，不使用例外狀況控制正常流程。
- 先讀取固定標頭與數值通訊埠，沒有訂閱 Agent 命中時不建立 IP/TCP 物件，也不具現化 Payload 陣列。
- 訂閱清單只在 Agent 啟停時建立不可變快照；逐封包路徑無鎖且不配置訂閱快照。
- 只有命中的內容檢查 Agent 存取 `Data` 時才複製 Payload；大型 Payload 不再受 128-byte 固定陣列限制。
- 無效或截斷封包直接記入 `iddscommunity.packets.malformed`，不建立 Stack Trace 或逐 Agent 廣播錯誤。

## 官方參考資料

- <https://learn.microsoft.com/windows/win32/winsock/sio-rcvall>
- <https://learn.microsoft.com/windows/win32/fwp/windows-filtering-platform-start-page>
- <https://learn.microsoft.com/windows/win32/fwp/best-practices>
- <https://learn.microsoft.com/windows/win32/fwp/ale-stateful-filtering>
- <https://learn.microsoft.com/dotnet/core/extensions/channels>
- <https://learn.microsoft.com/dotnet/standard/memory-and-spans/memory-t-usage-guidelines>
- <https://learn.microsoft.com/dotnet/api/system.buffers.binary.binaryprimitives?view=net-10.0>
- <https://learn.microsoft.com/dotnet/core/diagnostics/debug-highcpu>
- <https://github.com/basil00/WinDivert/issues/401>
- <https://www.nuget.org/packages/SharpPcap/6.3.1>
- <https://npcap.com/vs-winpcap>
