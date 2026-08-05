# Cyberarms 專案通用開發規範 (AGENTS.md)

本文件定義 Cyberarms 專案之技術架構、程式碼品質、編碼格式與 Git 提交規範。

---

## 1. 目標框架與專案結構

- **目標框架 (.NET 10.0)**：所有 C# 專案均須採用 SDK-Style 結構，並指定目標框架為 `<TargetFramework>net10.0-windows</TargetFramework>`。
- **解決方案格式 (`.slnx`)**：統一採用現代化 XML 格式之 [`Cyberarms.slnx`](file:///d:/Dev/Project/Application/Cyberarms/Cyberarms.slnx) 作為主要解決方案檔案。
- **專案相依性**：專案間互相關聯統一使用 SDK-Style `<ProjectReference>`，禁止使用舊型 GUID 專案參考。

---

## 2. NuGet 與套件管理

- **套件版本控制**：所有第三方與系統擴充套件均須使用最新穩定版本（例如 `System.Management.Automation`、`System.Data.SQLite.Core` 等）。
- **全域套件管理與弱點掃描**：於 [`Directory.Build.props`](file:///d:/Dev/Project/Application/Cyberarms/Directory.Build.props) 中統一管理跨專案相依套件版本與弱點升級（如 `Newtonsoft.Json 13.0.4+`），確保無已知高嚴重性安全性弱點。

---

## 3. 檔案換行字元與文字編碼規範

- **換行字元 (CRLF)**：Windows 環境下所有程式碼與文字檔案換行字元必須統一使用 `CRLF` (`\r\n`)，遵照 Git AutoCRLF 規範（已定義於 [`.gitattributes`](file:///d:/Dev/Project/Application/Cyberarms/.gitattributes) `* text=auto eol=crlf`）。
- **文字編碼 (UTF-8 / UTF-8 with BOM)**：
  - **C# 原始碼與專案資源 (`*.cs`, `*.csproj`, `*.resx`, `*.sln`, `*.slnx`, `*.ps1`)**：統一採用 `UTF-8 with BOM` (`utf-8-bom`)，確保 MSBuild、Roslyn (`csc`)、Visual Studio 與 Windows PowerShell 能無誤解析 CJK 雙位元組字元。
  - **標準設定與數據文件 (`*.json`, `*.md`, `*.yml`, `*.yaml`, `.git*`, `.editorconfig`)**：統一採用無 BOM 之標準 `UTF-8` (`utf-8`)，遵守 RFC 8259 及現代 Web 工具規範。

---

## 4. 程式碼品質與警告零容忍原則 (Zero Warnings / Zero Errors)

- **零警告零錯誤建置**：專案建置 `dotnet build Cyberarms.slnx` 必須達到 **0 個警告 (0 Warnings)、0 個錯誤 (0 Errors)**。
- **例外狀況處理**：
  - 重新拋出捕捉到的例外狀況時，必須使用 `throw;`，嚴禁使用 `throw ex;` 以避免破壞堆疊追蹤資訊 (CA2200)。
- **.NET 10 相容性與跨平台安全**：
  - 由於 .NET 10 不支援 secondary `AppDomain`，動態載入組件時須針對 .NET 10 提供單一 `AppDomain.CurrentDomain` 之相容邏輯，舊型 `AppDomain.CreateDomain` 與 `AppDomain.Unload` 須加上 `#if NETFRAMEWORK` 保護。
  - 防火牆等系統 COM 操作須優先使用 Microsoft 維護的 `Microsoft.Windows.CsWin32` 原始碼產生器，避免提交或散布預先產生的 Interop 二進位檔，並提供權限與例外備援機制。

---

## 5. Git 提交訊息規範 (Conventional Commits)

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

- **單元測試整合**：所有測試專案（`*.Test.csproj`）均採用 MSTest V3 / .NET 10 Test SDK，執行 `dotnet test Cyberarms.slnx` 必須全數綠燈通過。
- **環境獨立性**：測試腳本需具備環境獨立性，避免硬編碼絕對路徑或特定本機名稱；需適當處理非系統管理者權限（如 Windows EventLog 寫入與 Socket 監聽之例外捕捉）。
