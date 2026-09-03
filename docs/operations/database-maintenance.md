# SQLite 資料庫維護

IDDS 社群版的資料庫維護分為自動維護與必須停用服務的人工維護，所有成功操作都會寫入 `ProtectionAuditLog`。主資料庫、WAL 與維護備份均由 SQLite3 Multiple Ciphers 以 ChaCha20-Poly1305 加密；256 位元隨機資料庫金鑰另由 Windows DPAPI 本機範圍保護。

## 自動維護

服務依 `Protection:MaintenanceIntervalHours` 執行分批資料保留清理、可驗證線上備份、備份保留清理、`PRAGMA optimize` 與被動 WAL checkpoint。預設每 24 小時執行，保留 30 天且最多 10 份自動備份。可用下列設定調整：

- `Protection:AutomaticBackupEnabled`
- `Protection:MaintenanceIntervalHours`
- `Protection:BackupRetentionDays`
- `Protection:MaximumBackupCount`
- `Protection:IntrusionLogRetentionDays`
- `Protection:LockHistoryRetentionDays`
- `Protection:AuditRetentionDays`
- `Protection:CompletedEventRetentionDays`
- `Protection:MaintenanceBatchSize`

失敗的事件收件匣項目不會由資料保留工作刪除；仍被封鎖記錄引用的入侵事件也會保留。

## 管理介面

「設定 > 資料庫維護」提供狀態與可回收頁面檢視、快速或完整檢查、建立與再次驗證備份、備份清單、保留清理、最佳化、還原、空間回收及維護歷程。

還原與空間回收前必須停止 IDDS 社群版 Windows 服務。空間回收會先建立並驗證安全備份、檢查可用磁碟空間，以 `VACUUM INTO` 建立候選資料庫，完成完整性檢查後才原子替換；任何替換後錯誤都會改用回滾副本還原。請勿在操作期間手動移動或刪除資料庫、`-wal`、`-shm`、候選檔或回滾檔。

## 備份與復原責任

本機備份用於快速復原，並不取代異地備份。維護備份沿用目前安裝環境的資料庫金鑰，因此只有保留相同 DPAPI 金鑰檔的原 Windows 安裝環境才能開啟；單獨複製 `.db` 到另一台主機無法復原。正式環境應將已驗證備份與受控的系統復原資料納入具備存取控制、不可變保存及定期復原演練的整機災難復原方案。請勿以明文或未受保護形式複製、傳送資料庫金鑰。
