# Windows 特權整合測試

`PrivilegedWindows` 測試用來驗證一般單元測試無法涵蓋的 Windows 平台功能：Raw Socket、Application Event Log、Windows Firewall，以及專用測試服務的停止與啟動。

## 安全邊界

- 一般 `dotnet test Cyberarms.slnx` 不會執行任何系統異動；未設定 `CYBERARMS_RUN_PRIVILEGED_TESTS=1` 時，特權測試會標記為略過。
- 防火牆測試只建立停用且名稱唯一的 `Cyberarms Integration Test` 規則，並在 `finally` 中刪除。
- Event Log 測試只建立名稱唯一的測試 Source，並在 `finally` 中刪除。
- 服務測試只接受以 `Cyberarms Integration Test` 開頭的服務名稱，避免控制正式服務。

## 執行方式

以系統管理員身分開啟 PowerShell 7.4，再執行：

```powershell
.\scripts\run-privileged-windows-tests.ps1
```

若測試機已預先安裝專用服務，可一併驗證 SCM 停止與啟動：

```powershell
.\scripts\run-privileged-windows-tests.ps1 -ServiceName 'Cyberarms Integration Test Runtime'
```

專用服務必須部署在隔離測試機或一次性 Windows Runner，不得指向正式環境的 Cyberarms 服務。

## CI 分層

一般 CI 僅執行完整解決方案建置與非特權測試。特權工作使用獨立、可清除的 Windows Runner，提升權限後執行上述腳本；Runner 結束時還應確認沒有名稱或群組為 `Cyberarms Integration Test` 的殘留防火牆規則與 Event Log Source。
