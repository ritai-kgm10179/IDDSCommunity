# 第三方元件聲明與授權資訊（Third-Party Notices）

IDDS 社群版（IDDS Community）採用下列第三方開源元件與程式庫。

## SharpPcap

- 著作權：Tamir Gal, Chris Morgan and others
- 版本：6.3.1
- 授權條款：MIT
- 原始碼：https://github.com/dotpcap/sharppcap
- 套件：https://www.nuget.org/packages/SharpPcap/6.3.1

SharpPcap 僅在主機已安裝相容之 Npcap 或 WinPcap 驅動程式時使用。IDDS 社群版不散布或自動安裝任一原生擷取驅動程式。MIT 授權條款全文重現於下方章節，並獨立適用於 SharpPcap。

## PacketDotNet

- 著作權：Chris Morgan and contributors
- 版本：1.4.8
- 授權條款：Mozilla Public License 2.0
- 原始碼：https://github.com/dotpcap/packetnet/tree/690707ce56d6e9c266daf6236c4f76ac5035334c
- 授權文字：https://licenses.nuget.org/MPL-2.0
- 套件：https://www.nuget.org/packages/PacketDotNet/1.4.8

PacketDotNet 為 SharpPcap 未經修改的傳遞相依套件。其原始碼在封裝發行版本之對應修訂與完整 MPL-2.0 條款可由上方連結取得。

## Dapper

- 著作權：Copyright (c) 2019 Marc Gravell, Nick Craver, and contributors
- 授權條款：Apache License 2.0
- 原始碼：https://github.com/DapperLib/Dapper
- 套件：https://www.nuget.org/packages/Dapper/

本套件提供共用資料層中執行 SQLite 查詢所使用之輕量級物件關聯對應器（Object Mapper）。

Apache License 2.0 授權條款：https://www.apache.org/licenses/LICENSE-2.0

## Konscious.Security.Cryptography.Argon2

- 著作權：Copyright (c) Keef Aragon
- 授權條款：MIT
- 原始碼：https://github.com/kmaragon/Konscious.Security.Cryptography
- 套件：https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2/

本套件提供受控 Argon2id 密碼金鑰衍生實作，用於保護匯出之組態設定機密資料。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## MailKit

## MailKit

- 著作權：Copyright (c) 2013-2024 Jeffrey Stedfast and contributors
- 授權條款：MIT
- 原始碼：https://github.com/jstedfast/MailKit
- 套件：https://www.nuget.org/packages/MailKit/

MailKit 為開源跨平台 .NET 郵件用戶端程式庫。IDDS 社群版使用其進行 SMTP 電子郵件告警通知傳遞。MailKit 相依於 MimeKit（同作者，MIT 授權）。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Microsoft .NET 執行階段程式庫（Microsoft .NET Runtime Libraries）

- 著作權：Copyright (c) Microsoft Corporation.
- 授權條款：MIT
- 原始碼：https://github.com/dotnet/runtime
- 原始碼：https://github.com/dotnet/extensions

下列 Microsoft 套件依據 MIT 授權條款納入應用程式發行套件中：

- `Microsoft.Data.Sqlite.Core` — .NET 之 SQLite 資料庫驅動程式；原生 SQLite 提供者由 SQLite3 Multiple Ciphers 套件供應。
- `Microsoft.Extensions.Diagnostics.HealthChecks` — 託管服務健康狀態檢查基礎架構。
- `Microsoft.Extensions.Hosting` 與 `Microsoft.Extensions.Hosting.WindowsServices` — 通用主機與 Windows 服務整合。
- `Microsoft.Extensions.Options.DataAnnotations` — 透過資料註解進行選項驗證。
- `Microsoft.Extensions.Resilience` — 復原管線原語（Polly 整合）。
- `System.Configuration.ConfigurationManager` — 組態設定檔存取。
- `System.Diagnostics.EventLog` — Windows 事件檢視器（Event Log）讀取與寫入。
- `System.Management` — WMI 與 Windows 管理結構檢測。
- `System.Security.Cryptography.ProtectedData` — Windows DPAPI 本機資料保護。
- `System.ServiceProcess.ServiceController` — Windows 服務狀態控制。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Microsoft.Windows.CsWin32

- 著作權：Copyright (c) Microsoft Corporation.
- 授權條款：MIT
- 原始碼：https://github.com/microsoft/CsWin32
- 套件：https://www.nuget.org/packages/Microsoft.Windows.CsWin32/

本套件作為私有建置相依項目參考，負責產生編譯至 IDDS 社群版中之 Windows 防火牆 COM 繫結原始碼。產生器套件本身不包含於應用程式發行散布套件中。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## SQLite3 Multiple Ciphers 與 SQLitePCLRaw

- 著作權：Copyright (c) 2023-2026 Ulrich Telle and contributors
- 著作權：Copyright (c) 2014-2026 Eric Sink and contributors
- 授權條款：MIT
- 原始碼：https://github.com/utelle/SQLite3MultipleCiphers
- 套件：https://www.nuget.org/packages/SQLite3MC.PCLRaw.bundle/

`SQLite3MC.PCLRaw.bundle` 提供 SQLite3 Multiple Ciphers 原生資料庫引擎與 SQLitePCLRaw 繫結，供 `Microsoft.Data.Sqlite.Core` 加密 SQLite 主資料庫、日誌及備份。上游套件同時包含各加密演算法來源專案的授權；完整逐檔授權以上游儲存庫的授權文件為準。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## 外部威脅情報來源與清單訂閱（External Threat Feeds & Intelligence Sources）

IDDS 社群版支援自動訂閱外部威脅情報來源以進行主動防禦 IP 封鎖。系統支援利用下列開放與社群威脅情報清單：

### IPsum
- 作者：Miroslav Stampar (@stamparm)
- 授權條款：MIT License
- 原始碼：https://github.com/stamparm/ipsum
- 說明：IPsum 為聚合威脅情報來源，彙整來自 30 多個公有黑名單的惡意 IPv4 位址清冊。

### AbuseIPDB
- 維護者：AbuseIPDB (Marconi Software)
- 使用條款：https://www.abuseipdb.com/terms
- 說明：社群驅動之 IP 黑名單資料庫。使用者可填入自備之註冊 API 金鑰以存取黑名單端點並遵守 AbuseIPDB 用量配額規範。

### Spamhaus DROP (Don't Route Or Peer)
- 維護者：The Spamhaus Project
- 使用條款：https://www.spamhaus.org/drop/
- 說明：由 Spamhaus 維護之高風險惡意網路網段諮詢棄用清單，專用於網路防禦。

### CINS Army
- 維護者：Sentinel IPS / CINS Score
- 授權條款：https://cinsscore.com/
- 說明：由 Sentinel 入侵感測節點分析彙整之惡意攻擊來源清冊。

### Blocklist.de
- 維護者：Blocklist.de
- 使用條款：https://www.blocklist.de/
- 說明：由 Fail2Ban 社群回報彙整之惡意攻擊 IP 位址清單。
