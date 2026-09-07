---
name: release-gate
description: >-
  IDDS Community 版本發行前置檢驗門檻（Release Gate）。當使用者準備標記版本標籤 (Git Tag)、執行發行審查、確認零警告建置、執行全套單元測試、檢查多語系對齊或稽核 GPG 簽章時使用此技能。
---

# IDDS Community 版本發行前置檢驗門檻手冊

本技能規範 IDDS Community 發布新版本前必須通過的五道品質與安全性檢驗門檻，確保符合專案開發規範（[AGENTS.md](../../AGENTS.md)）之零容忍原則與發行供應鏈安全。

---

## 發行前五大檢驗門檻

### 門檻 1：零警告、零錯誤建置驗證

專案建置必須達到 **0 個警告 (0 Warnings)、0 個錯誤 (0 Errors)**，且限定單一建置節點：

`powershell
dotnet build IDDSCommunity.slnx -m:1
`

若有任何編譯警告或 Roslyn 分析警告，一律視為門檻失敗，必須先行修復方可繼續。

---

### 門檻 2：全套單元測試 100% 綠燈通過

執行全方案單元測試，所有測試案例必須全數通過：

`powershell
dotnet test IDDSCommunity.slnx -m:1
`

- 涵蓋核心服務、管理主控台、全部 Security Agents 以及共享類別庫之測試專案。
- 測試過程中嚴格禁止跳過或忽略任何既有失敗之測試案例。

---

### 門檻 3：多語系語系檔對等性檢查 (Resource Parity)

執行語系管理員測試，確保正體中文、英文等各語系資源檔鍵值完全對齊無遺漏：

`powershell
dotnet test tests/IDDSCommunity.IntrusionDetection.Shared.Test/IDDSCommunity.IntrusionDetection.Shared.Test.csproj --filter FullyQualifiedName~LanguageManagerTest -m:1
`

---

### 門檻 4：檔案編碼與換行格式稽核 (Encoding & EOL)

- **C# 原始碼與專案資源 (*.cs, *.csproj, *.resx, *.slnx, *.ps1)**：必須為 UTF-8 with BOM，換行字元為 CRLF。
- **標準設定與 Markdown 文件 (*.json, *.md, *.yml, .git*, .editorconfig)**：必須為無 BOM 之 UTF-8，換行字元為 CRLF。

---

### 門檻 5：GPG 簽署標籤與提交驗證 (Release Signature)

1. **Annotated Tag 命名格式**：正式發行標籤必須嚴格符合 X.Y.Z 格式（如 3.0.0）。
2. **GPG 簽署強制性**：
   - 提交與標籤必須使用授權之 GPG 金鑰進行簽署：
     `powershell
     # 驗證 HEAD 提交簽章
     git log -1 --show-signature
     # 建立 GPG 簽署標籤範例
     git tag -s vX.Y.Z -m "Release vX.Y.Z"
     # 驗證標籤簽章
     git tag -v vX.Y.Z
     `
3. **CI 確切 SHA 鎖定**：發布流水線必須驗證 GitHub 回報之 OpenPGP 簽章，並直接使用已驗證之 commit SHA 進行封裝，禁止以可變標籤名稱重新解析。