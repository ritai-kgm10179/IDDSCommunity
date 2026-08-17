# 發布流程

## 持續整合

推送至 `main` 或建立以 `main` 為目標的 Pull Request 時，GitHub Actions 會在 `windows-2025` 執行：

- Release 組態還原、建置及完整的一般測試。
- 需要系統管理員權限、Windows 服務與事件記錄檔的整合測試。
- 上傳 TRX 測試結果，保留 14 天供失敗診斷。

兩個工作皆通過才代表目前提交已完成自動化驗證。建議在 GitHub 分支保護規則中，將 `Build and test` 與 `Privileged Windows integration tests` 設為 `main` 的必要狀態檢查。

## 建立正式版本

發布工作只接受指向版本提交、且 GitHub 驗證成功的 GPG 簽署 annotated tag。版本格式固定為 `vX.Y.Z`，並須與安裝程式專案的 `<Version>` 完全一致。

以 3.0.0 為例：

```powershell
git tag -s v3.0.0 -m "IDDS Community 3.0.0"
git tag -v v3.0.0
git push origin v3.0.0
```

Tag 推送後，`Release installer` workflow 會：

1. 驗證 tag 格式、GPG 簽章、目標提交與專案版本，並傳出已驗證的確切 commit SHA 供後續步驟 checkout（避免驗證後 tag 被移動而繞過檢查）。
2. 重新執行 Release 建置及一般測試。
3. 建立 `win-x64` 與 `win-arm64` 的 self-contained 安裝套件，因此使用者不必另外安裝 .NET Runtime。
4. 為每個平台產生目前規格的 SPDX 3.0 SBOM 與相容性格式 SPDX 2.2 SBOM，並納入安裝套件內容。
5. 產生 ZIP、SBOM 與 SHA-256 校驗檔，並建立 GitHub artifact attestation。
6. 建立 GitHub Release 並附加所有安裝套件、SBOM 與校驗檔。

可使用 GitHub CLI 驗證下載套件的來源證明：

```powershell
gh attestation verify .\idds-community-3.0.0-win-x64.zip --repo ritai-kgm10179/IDDSCommunity
```

若 workflow 是從 `workflow_dispatch` 手動啟動，輸入值必須是已存在且符合上述要求的簽署 tag；手動執行不會建立或改寫 tag。
