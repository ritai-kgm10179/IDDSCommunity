# 資產來源與產製紀錄

IDDS Community 發行內容不得包含來源不明或沿用舊專案的圖檔。

目前 `*.png` 與 `*.gif` UI 資產由 `tools/Generate-OriginalAssets.ps1` 從透明畫布及基本幾何圖形重新產製。產製程序僅讀取既有檔案的畫布寬高，以維持 WinForms 版面契約，不讀取或臨摹既有像素、構圖或圖像內容。所有符號、配色與繪製規則均為本專案重新定義，產出可由原始碼完整重現。

`assets/branding/idds-community.ico` 為 IDDS Community 專案品牌資產，不是舊專案資源；其使用範圍僅限應用程式與安裝程式圖示。

程式碼、文件、視覺規則及資產產製流程均由 AI 協作產生，並由維護者負責檢視、測試與發布決策。
