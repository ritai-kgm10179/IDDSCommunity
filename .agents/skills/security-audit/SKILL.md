---
name: security-audit
description: >-
  IDDS Community 安全性合規稽核手冊。當使用者要求進行程式碼安全審查、驗證防禦機制 (Socket-level Bogon/IMDS、ChatOps、RateLimiter、CSV/Webhook 注入防護) 或比對 SQLite3MC 原生 CVE 弱點時使用此技能。
---

# IDDS Community 安全防禦與弱點合規稽核手冊

本技能提供 IDDS Community 系統性安全審查之檢驗步驟與基準檢核清單，涵蓋網路連線邊界、介面存取控制、注入防禦及第三方原生核心之 CVE 弱點稽核。

---

## 安全稽核檢驗清單 (Security Audit Checklist)

### 1. 連線層 Bogon 與 IMDS 防禦稽核 (Socket-Level Defense)

- **目標檔案**：[`src/IDDSCommunity.IntrusionDetection.Shared/Net/HttpClientHelper.cs`](../../src/IDDSCommunity.IntrusionDetection.Shared/Net/HttpClientHelper.cs)
- **檢驗項目**：
  - [ ] 所有外部情資抓取與 Webhook 連線必須透過統一 `HttpClientHelper` 實例發起。
  - [ ] 底層必須實作 `SocketsHttpHandler.ConnectCallback`，在 TCP Socket 連線握手當下直接檢查目標 IP 位址，有效阻斷 DNS Rebinding 繞過前置驗證。
  - [ ] 強制攔截私有網段（RFC 1918）、CGNAT（RFC 6598）、本機迴路（127.0.0.0/8, ::1）、廣播及雲端中繼資料位址（`169.254.169.254`）。

---

### 2. ChatOps RFC 9110 安全方法約束稽核

- **目標範圍**：ChatOps 端點與 Webhook 監聽器
- **檢驗項目**：
  - [ ] 所有唯讀狀態查詢（如 `/status`、`/whois`、`/stats`）僅能使用 RFC 9110 定義之安全方法（`GET`、`HEAD`、`OPTIONS`）。
  - [ ] 具有副作用之操作（如 IP 解鎖、手動加入黑名單）一律禁止透過 GET 方法觸發，必須使用 `POST` 或 `DELETE`。
  - [ ] 狀態變更請求必須具備有效防偽權杖（Token）或 CSRF 鑑別機制。

---

### 3. API 金鑰失敗嘗試頻率限制稽核 (Brute-Force Defense)

- **目標檔案**：[`src/IDDSCommunity.IntrusionDetection.Shared/Security/FailedAttemptsRateLimiter.cs`](../../src/IDDSCommunity.IntrusionDetection.Shared/Security/FailedAttemptsRateLimiter.cs)
- **檢驗項目**：
  - [ ] API 鑑別失敗時計數器必須以來源 IP 作為隔離維度。
  - [ ] 連續失敗達到設定門檻（如 5 次）後，必須實施冷卻期或指數退避（Exponential Backoff）。
  - [ ] 記憶體中之失敗計數字典必須具備自動過期與記憶體清理機制，防止記憶體耗盡（Memory Exhaustion）。

---

### 4. 監控指標預設迴路繫結稽核 (Loopback Metrics)

- **目標檔案**：Prometheus Metrics 暴露端點
- **檢驗項目**：
  - [ ] 指標監聽端點（`/metrics`）預設僅能監聽本機迴路位址（`127.0.0.1` 與 `::1`）。
  - [ ] 嚴禁未經身分驗證直接對外部網路介面（`0.0.0.0`）開放未授權之內部防護計量資料。

---

### 5. 匯出 CSV 公式注入防禦稽核 (CWE-1236)

- **目標範圍**：事件記錄與日誌匯出模組
- **檢驗項目**：
  - [ ] 任何文字欄位匯出為 CSV 格式時，若首字元包含 `=, +, -, @, \t, \r`，必須前置單引號（`'`）跳脫。
  - [ ] 欄位包含逗號、引號或換行時必須正確以雙引號封裝並跳脫內含引號。

---

### 6. Webhook 視覺偽冒與長度截斷稽核 (Webhook Sanitization)

- **目標檔案**：[`src/IDDSCommunity.IntrusionDetection.Shared/Notifications/WebhookPayloadBuilder.cs`](../../src/IDDSCommunity.IntrusionDetection.Shared/Notifications/WebhookPayloadBuilder.cs)
- **檢驗項目**：
  - [ ] 移除所有 Unicode 雙向控制字元（`\u202A`~`\u202E`、`\u2066`~`\u2069`），防止文字反向視覺欺瞞。
  - [ ] Teams 與 Discord 訊息跳脫方括號（`[` 與 `]`），防止非預期 Markdown 超連結釣魚。
  - [ ] Discord 負載明確指定 `"allowed_mentions": { "parse": [] }`，防止濫用廣播通知。
  - [ ] LINE Messaging API 嚴格執行 5,000 字元截斷並修剪接收者識別碼空白字元。

---

### 7. 原生 SQLite 與 SQLite3MultipleCiphers C 引擎 CVE 查核

- **目標套件**：`SQLite3MC.PCLRaw.bundle`
- **檢驗項目**：
  - [ ] 查閱目前所依賴 `SQLite3MC.PCLRaw.bundle` 封裝之 upstream SQLite 核心版本。
  - [ ] 比對 [SQLite 官方 CVE 清單](https://www.sqlite.org/cves.html) 確認核心無已知重大遠端代碼執行或記憶體損毀弱點。
  - [ ] 查閱 [SQLite3MultipleCiphers Releases](https://github.com/utelle/SQLite3MultipleCiphers/releases) 追蹤最新安全性更新與修正。