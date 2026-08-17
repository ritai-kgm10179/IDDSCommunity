# 生產環境驗收矩陣

本矩陣將「程式碼存在」與「功能已經過驗收」分開記錄。任何正式版本均須以同一提交完成所有必備項目；需要系統管理員權限或長時間執行的項目，不得以一般單元測試結果替代。

| 領域 | 自動化驗證 | 驗收條件 | 執行位置 |
| --- | --- | --- | --- |
| 建置與套件 | `dotnet build IDDSCommunity.slnx --configuration Release --disable-build-servers -m:1 -p:UseSharedCompilation=false` | 0 warnings、0 errors | 本機、CI |
| 一般回歸 | `dotnet test IDDSCommunity.slnx --configuration Release --disable-build-servers -m:1` | 全部通過；特權案例僅可明確跳過 | 本機、CI |
| Agent 解析 | 各 Agent 的解析器測試；每個 `agents/*` 專案均有對應之獨立同名 `*.Test.csproj`，另有共用 `Authentication.Common` 框架測試涵蓋滑動時間窗與門檻判斷邏輯 | 成功登入不得計為失敗；格式變體、無效位址與輪替邊界皆有案例 | CI |
| 計數與報表 | 查詢、每日郵件與管理工具匯出測試 | 同一時間區間、事件類型與 Agent 篩選產生相同總數 | CI |
| 設定移轉 | Argon2id 與 AES-256-GCM 匯入／匯出測試 | 正確密碼往返一致；錯誤密碼、竄改與超限參數必須拒絕 | CI |
| SQLite 維護 | 完整性、備份、還原、保留與空間回收測試 | 不損失已提交資料；失敗可回復且有診斷紀錄 | CI |
| 安全事件管線 | `TestCategory=Stability` | 並行突發事件全數排空；單筆失敗不阻斷後續事件；失敗事件可重播 | CI |
| Windows 防火牆 | `scripts/run-ci-windows-integration-tests.ps1` | 真實規則可建立、查詢、由事件管線觸發並清除；測試後無殘留 | 提升權限 Runner |
| Raw Socket／Event Log／服務 | 同一特權整合測試腳本 | 擷取可啟停、事件來源可寫入、專用測試服務可啟停 | 提升權限 Runner |
| 安裝程式 | `build-setup.ps1` 與安裝驗證 | x64／ARM64 產物可安裝、啟動、移除；文件、Runtime、SBOM 與雜湊齊全 | 發行 CI、乾淨 VM |
| UI 與 i18n | UI 自動化檢查清單與人工 DPI 驗收 | 100%、125%、150%、200% DPI 無裁切；所有支援語系無裸露資源鍵 | 乾淨 VM |
| 供應鏈 | 標籤發布工作流程 | 簽署標籤與提交驗證成功；Release、SPDX 3.0／2.2、SHA-256 與 attestation 齊全 | 發行 CI |

## 負載與耐久驗收

- 每次提交執行短時間、可重現的並行突發與錯誤隔離測試，並保留 TRX 診斷產物。
- 發行候選版本須在接近實際部署的 Windows Server VM 以 Release 組態執行尖峰與長時間耐久測試；至少記錄事件接受量、處理量、拒絕量、佇列深度、處理延遲、失敗量、SQLite 大小、記憶體與 CPU。
- 耐久測試的通過條件是負載停止後能在既定逾時內排空、沒有未解釋的事件遺失、資源用量沒有持續單調惡化，並能在注入單筆處理失敗後繼續服務。
- GitHub Hosted Runner 的短測試只提供回歸證據，不代表完成真實網路流量、長時間資源耗用或安裝升級的生產驗收。

## 失敗處理

任何必備列失敗時，版本不得標示為 production-ready。CI 必須保留 TRX 或等效診斷產物；修正後應重新執行整個相關層級，而不是只重跑單一失敗斷言。
