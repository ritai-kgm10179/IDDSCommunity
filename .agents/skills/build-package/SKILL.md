---
name: build-package
description: >-
  IDDS Community 自包含安裝包封裝與建置產物驗證流程。當使用者要求建立安裝程式、驗證自包含安裝包 (Setup.exe)、清理建置伺服器程序或稽核建置產物擁有者時使用此技能。
---

# IDDS Community 安裝包建置與產物驗證手冊

本技能規範 IDDS Community 產出獨立自包含（Self-Contained）Windows 安裝程式之標準作業流程與驗證檢核點，確保符合專案開發規範第 7 節及發行供應鏈安全要求。

---

## 核心限制與安全準則

1. **非提升權限作業**：安裝包之還原、編譯、單元測試與封裝打包程序，一律必須使用目前登入使用者的**非提升權限（Non-Elevated）**工作階段執行；嚴禁直接以系統管理員身分執行整套建置腳本。
2. **限制單一建置節點**：MSBuild 必須強制指定單一節點（-m:1）、停用平行還原，並於封裝流程停用節點重用（/nr:false），避免背景殘留無主之 dotnet.exe 或編譯器伺服器程序。
3. **CI 與本機一致性**：無論本機封裝或 GitHub Actions 發布流水線，統一呼叫專案根目錄之 [uild-setup.ps1](../../build-setup.ps1)，不得另行維護行為相異之封裝邏輯。

---

## 執行封裝流程

### 步驟 1：執行標準封裝腳本

開啟 PowerShell（非管理員權限），於專案根目錄執行：

`powershell
.\build-setup.ps1 -Runtime win-x64
`

> **注意**：如需針對 ARM64 架構打包，請指定 -Runtime win-arm64。

---

## 建置產物檢核表 (Validation Checklist)

封裝完成後，必須逐項驗證以下產物與特性：

- [ ] **安裝程式檔案**：產物目錄必須存在且命名為 Setup.exe。
- [ ] **PDB 嵌入規則**：核心模組（IDDSCommunity.IntrusionDetection.Service 與 IDDSCommunity.IntrusionDetection.Admin）採 <DebugType>embedded</DebugType>，不應產生亦不得要求包含獨立之 .pdb 符號檔。
- [ ] **雙規格 SBOM**：
  - [ ] 產物必須包含 SPDX 3.0 格式之軟體物料清單（SBOM）。
  - [ ] 產物必須包含向下相容之 SPDX 2.2 格式 SBOM。
  - [ ] 產出 SHA-256 總和檢查碼以供來源證明（Attestation）驗證。
- [ ] **產物擁有者稽核**：檢查 rtifacts/、in/ 與 obj/ 新增檔案之擁有者是否為目前登入使用者，無權限污染情況。

---

## 建置後程序清理 (Post-Build Cleanup)

封裝與驗證完成後，必須確認無殘留背景編譯器工作進程：

`powershell
# 停止並清理 dotnet / MSBuild 建置節點伺服器
dotnet build-server shutdown
`

僅清理本次建置產生的暫存輸出，嚴禁刪除原始碼、使用者工作區異動或正式安裝包。