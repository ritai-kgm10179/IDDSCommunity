# Cyberarms 發布 Go／No-Go 檢查表

## 自動化門檻

- `dotnet build Cyberarms.slnx` 必須為 0 警告、0 錯誤。
- `dotnet test Cyberarms.slnx` 不得有失敗；略過測試必須逐項說明環境原因。
- NuGet 弱點掃描不得存在已知高嚴重性弱點。
- RESX 鍵、UTF-8 BOM 與 CRLF 檢查必須通過。
- 封包壓力測試必須維持容量上限，接收數等於派送數加丟棄數。

## Windows 特權驗證

- 在隔離 Windows Runner 執行 `scripts/run-privileged-windows-tests.ps1`。
- 驗證服務安裝、啟動、停止、故障回報與重新啟動。
- 驗證 Raw Socket、Event Log 與獨立防火牆規則，並確認沒有測試資源殘留。

## 執行期健康

- `cyberarms-runtime` 健康檢查至少為 Healthy；Degraded 必須有核准的已知原因。
- 監看 `Cyberarms.IntrusionDetection` Meter 的 received、dispatched 與 dropped 指標。
- 長時間執行不得出現持續性記憶體、Agent 效能歷程或封包佇列增長。

## No-Go 條件

任何建置或測試失敗、資料庫遷移無法回復、外掛路徑驗證失敗、正式防火牆規則受到測試影響，或特權測試後有資源殘留，都必須停止發布。
