# IDDS Community 專案通用開發規範 (AGENTS.md)

本文件定義 IDDS Community 專案之技術架構、程式碼品質、編碼格式與 Git 提交規範。

---

## 1. 目標框架與專案結構

- **目標框架 (.NET 10.0)**：所有 C# 專案均須採用 SDK-Style 結構，並指定目標框架為 `<TargetFramework>net10.0-windows</TargetFramework>`。
- **解決方案格式 (`.slnx`)**：統一採用現代化 XML 格式之 [`IDDSCommunity.slnx`](IDDSCommunity.slnx) 作為主要解決方案檔案。
- **專案相依性**：專案間互相關聯統一使用 SDK-Style `<ProjectReference>`，禁止使用舊型 GUID 專案參考。

---

## 2. NuGet 與套件管理

- **套件版本控制**：所有第三方與系統擴充套件均須使用最新穩定版本（例如 `System.Management.Automation`、`System.Data.SQLite.Core` 等）。
- **全域套件管理與弱點掃描**：於 [`Directory.Build.props`](Directory.Build.props) 中統一管理跨專案相依套件版本與弱點升級（如 `Newtonsoft.Json 13.0.4+`），確保無已知高嚴重性安全性弱點。

---

## 3. 檔案換行字元與文字編碼規範

- **換行字元 (CRLF)**：Windows 環境下所有程式碼與文字檔案換行字元必須統一使用 `CRLF` (`\r\n`)，遵照 Git AutoCRLF 規範（已定義於 [`.gitattributes`](file:///d:/Dev/Project/Application/IDDSCommunity/.gitattributes) `* text=auto eol=crlf`）。
- **文字編碼 (UTF-8 / UTF-8 with BOM)**：
  - **C# 原始碼與專案資源 (`*.cs`, `*.csproj`, `*.resx`, `*.sln`, `*.slnx`, `*.ps1`)**：統一採用 `UTF-8 with BOM` (`utf-8-bom`)，確保 MSBuild、Roslyn (`csc`)、Visual Studio 與 Windows PowerShell 能無誤解析 CJK 雙位元組字元。
  - **標準設定與數據文件 (`*.json`, `*.md`, `*.yml`, `*.yaml`, `.git*`, `.editorconfig`)**：統一採用無 BOM 之標準 `UTF-8` (`utf-8`)，遵守 RFC 8259 及現代 Web 工具規範。

---

## 4. 程式碼品質與警告零容忍原則 (Zero Warnings / Zero Errors)

- **零警告零錯誤建置**：專案建置 `dotnet build IDDSCommunity.slnx` 必須達到 **0 個警告 (0 Warnings)、0 個錯誤 (0 Errors)**。
- **例外狀況處理**：
  - 重新拋出捕捉到的例外狀況時，必須使用 `throw;`，嚴禁使用 `throw ex;` 以避免破壞堆疊追蹤資訊 (CA2200)。
- **.NET 10 相容性與跨平台安全**：
  - 由於 .NET 10 不支援 secondary `AppDomain`，動態載入組件時須針對 .NET 10 提供單一 `AppDomain.CurrentDomain` 之相容邏輯，舊型 `AppDomain.CreateDomain` 與 `AppDomain.Unload` 須加上 `#if NETFRAMEWORK` 保護。
  - 防火牆等系統 COM 操作須優先使用 Microsoft 維護的 `Microsoft.Windows.CsWin32` 原始碼產生器，避免提交或散布預先產生的 Interop 二進位檔，並提供權限與例外備援機制。

---

## 5. Git 提交訊息規範 (Conventional Commits)

- **GPG 簽署**：所有提交（包含修正提交與合併提交）都必須使用專案成員已驗證的 GPG 金鑰簽署；禁止將未簽署提交推送至共用分支。
- **約定式提交規範**：提交訊息必須符合 Conventional Commits 格式，包含**主旨 (Subject)** 與**詳細內文 (Body)**。
- **語言與用語**：必須使用**正體中文（台灣地區用語）**撰寫。
- **格式範例**：
  ```text
  <type>(<scope>): <主旨描述>

  - <內文条目 1>
  - <內文條目 2>
  ```
  常見 Type 包含：`feat`（新功能/重構）、`fix`（修復）、`docs`（文件）、`style`（格式）、`refactor`（重構）、`test`（測試）、`chore`（雜務/建置設定）。

---

## 6. 單元測試驗證

- **單元測試整合**：所有測試專案（`*.Test.csproj`）均採用 MSTest V3 / .NET 10 Test SDK，執行 `dotnet test IDDSCommunity.slnx` 必須全數綠燈通過。
- **環境獨立性**：測試腳本需具備環境獨立性，避免硬編碼絕對路徑或特定本機名稱；需適當處理非系統管理者權限（如 Windows EventLog 寫入與 Socket 監聽之例外捕捉）。

---

## 7. 自包含安裝包與建置程序清理

- **RID 專屬還原**：建立自包含安裝包前，必須針對目標 RID（`win-x64` 或 `win-arm64`）執行還原；統一由 [`build-setup.ps1`](build-setup.ps1) 負責，不得假設一般方案還原已包含執行階段資產。
- **限制建置節點**：大量專案的還原、建置、測試與發佈必須使用單一 MSBuild 節點（`-m:1`）、停用平行還原，並在封裝流程停用節點重用，避免殘留大量 `dotnet.exe`、`MSBuild.exe` 或編譯器伺服器程序。
- **CI 與本機一致**：標籤發布 CI 必須直接呼叫相同的 `build-setup.ps1`，且不得另行維護行為不同的封裝步驟。
- **工作區清理**：完成驗證後，僅能清理由本次建置產生且已確認不再使用的建置伺服器、測試輸出與暫存產物；不得刪除原始碼、使用者變更或正式安裝包。清理建置程序前須先確認沒有仍在執行的 IDE、測試或發佈工作。
- **非提升權限建置**：一般還原、建置與測試必須使用目前登入使用者的非提升權限工作階段；只有安裝服務、修改防火牆或安裝程式驗證等確實需要系統權限的短生命週期操作才可提升權限，禁止以系統管理員身分執行整套建置。
- **產物擁有者稽核**：完成本機驗證後，須檢查新產生的 `bin`、`obj`、測試結果及暫存目錄是否由目前登入使用者擁有。若擁有者異常，僅能在確認目標位於本專案且屬可重建產物後刪除並以非提升權限重建；禁止對儲存庫根目錄、原始碼或使用者既有檔案遞迴接管所有權或重設 ACL。
- **PDB 嵌入（Service 與 Admin）**：`IDDSCommunity.IntrusionDetection.Service` 與 `IDDSCommunity.IntrusionDetection.Admin` 採用 `<DebugType>embedded</DebugType>`，不會產生獨立的 `.pdb` 符號檔；安裝包內容驗證不得要求 `.pdb` 存在，部署產物目錄亦無需包含 `.pdb` 檔案。安裝包輸出安裝程式統一命名為 `Setup.exe`。

---

## 8. 設定機密與發行供應鏈

- **設定匯出加密**：設定套件中的機密資料必須使用 Argon2id 衍生 256 位元金鑰，再以 AES-256-GCM 加密及驗證；密碼衍生參數必須設有匯入上限，且參數、格式版本與演算法識別必須納入 AAD，禁止未經明確格式升版而降級至 PBKDF2。
- **簽署發行標籤**：正式發行僅能由符合 `vX.Y.Z` 的 GPG 簽署 annotated tag 觸發，CI 必須驗證 GitHub 回報的 OpenPGP 簽章與標籤所指提交。
- **SBOM 與來源證明**：每個發行平台的安裝包必須同時產生目前規格的 SPDX 3.0 SBOM 與相容性格式 SPDX 2.2 SBOM，將兩者納入安裝包及 GitHub Release 附件，並發布 SHA-256 雜湊與 GitHub artifact attestation。

---

## 9. 來源 IP 例外設定

- **單一設定來源**：所有 Agent 的來源 IP 例外均須使用管理工具「設定 → 安全網路」的全域允許清單；Agent 自訂設定不得另建重複的 IP 排除欄位。
- **位址格式**：安全網路必須支援單一 IPv4、單一 IPv6、IPv4 CIDR 與 IPv6 CIDR；本機位址由服務層統一辨識，不得要求使用者在每個 Agent 重複輸入。
