# 圖片資產來源與驗證

IDDS Community 發行內容中的圖片分成以下兩類：

- `assets/branding/idds-community.ico`：專案原創、AI 協作設計的品牌圖示。
- 各專案 `Resources`／`res` 目錄中的 PNG 與 GIF：由 [`tools/Generate-OriginalAssets.ps1`](tools/Generate-OriginalAssets.ps1) 使用透明畫布、基本幾何圖形及專案色彩重新產生；產生器只讀取既有畫布尺寸以維持 WinForms 版面契約，不讀取或臨摹既有像素內容。

[`assets/asset-provenance.json`](assets/asset-provenance.json) 記錄所有 Git 追蹤圖片的路徑、SHA-256、來源分類與產生來源。CI 會執行 [`scripts/test-asset-provenance.ps1`](scripts/test-asset-provenance.ps1)，確認：

- 所有追蹤圖片都已列入清冊，沒有未申報圖片。
- 圖片內容雜湊與清冊一致。
- 禁止的舊資產名稱沒有重新進入儲存庫。
- 程式產生圖片均指向目前的 IDDS Community 資產產生器。

圖片變更後，維護者必須執行 `./scripts/update-asset-provenance.ps1` 更新清冊，檢視差異後再提交。來源清冊提供可重現的工程稽核證據，但不構成商標或著作權法律意見。
