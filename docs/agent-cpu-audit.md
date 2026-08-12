# Agent 常駐 CPU 稽核與修正計畫

稽核日期：2026-08-12

## 官方判準

- Microsoft 的 `SIO_RCVALL` 文件指出，啟用後會把指定介面收到的所有 IP 封包交給 Raw Socket。因此，為每個 Agent 建立 Raw Socket 會重複接收相同流量。
- Microsoft Windows Filtering Platform（WFP）最佳實踐建議使用 Application Layer Enforcement（ALE），並明確指出逐封包層級的過濾較慢。
- WFP 的 ALE 狀態式過濾只需分類連線的第一個封包，可大幅降低分類次數；需要封包內容檢查時，Microsoft 建議使用 Stream／Datagram Data 層。
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

1. 本次完成：將現有 Raw Socket 後端集中成每張 IPv4 介面單一執行個體，只解析一次 IP/TCP 標頭，再依通訊埠和方向分派。
2. 本次完成：維持 bounded Channel、丟包計數與錯誤隔離，避免高流量造成無界排隊。
3. 本次完成：讓四個內容檢查 Agent 直接使用已解析 TCP 事件，消除第二次標頭解析。
4. 後續重大版本：建立並簽署 WFP Callout Driver，使用 ALE 建立流量範圍、使用 Stream 層取得真正需要的應用層資料，並把目前的共用擷取器保留為可替換後端介面。
5. 發行前驗證：在相同流量回放下比較 CPU、配置率、GC、接收／分派／丟棄封包數，並以長時間 soak test 驗證停止、重新啟動及 Agent 動態載入。

## 官方參考資料

- <https://learn.microsoft.com/windows/win32/winsock/sio-rcvall>
- <https://learn.microsoft.com/windows/win32/fwp/windows-filtering-platform-start-page>
- <https://learn.microsoft.com/windows/win32/fwp/best-practices>
- <https://learn.microsoft.com/windows/win32/fwp/ale-stateful-filtering>
- <https://learn.microsoft.com/dotnet/core/extensions/channels>
