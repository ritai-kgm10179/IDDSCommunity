# Third-Party Notices

IDDS Community uses the following third-party components.

## Dapper

- Copyright (c) 2019 Marc Gravell, Nick Craver, and contributors
- License: Apache License 2.0
- Source: https://github.com/DapperLib/Dapper
- Package: https://www.nuget.org/packages/Dapper/

The package provides the lightweight object mapper used for SQLite query execution within the shared data layer.

Apache License 2.0: https://www.apache.org/licenses/LICENSE-2.0

## Konscious.Security.Cryptography.Argon2

- Copyright (c) Keef Aragon
- License: MIT
- Source: https://github.com/kmaragon/Konscious.Security.Cryptography
- Package: https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2/

The package provides the managed Argon2id password-based key derivation implementation used to protect exported configuration secrets.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## MailKit

- Copyright (c) 2013-2024 Jeffrey Stedfast and contributors
- License: MIT
- Source: https://github.com/jstedfast/MailKit
- Package: https://www.nuget.org/packages/MailKit/

MailKit is an open source cross-platform .NET mail client library. IDDS Community uses it for SMTP notification delivery. MailKit depends on MimeKit (same author, MIT license).

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Microsoft .NET Runtime Libraries

- Copyright (c) Microsoft Corporation.
- License: MIT
- Source: https://github.com/dotnet/runtime
- Source: https://github.com/dotnet/extensions

The following packages from Microsoft are included in the application distribution under the MIT License:

- `Microsoft.Data.Sqlite.Core` — SQLite database driver for .NET；原生 SQLite 提供者由 SQLite3 Multiple Ciphers 套件供應。
- `Microsoft.Extensions.Diagnostics.HealthChecks` — Health check infrastructure for hosted services.
- `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices` — Generic host and Windows Service integration.
- `Microsoft.Extensions.Options.DataAnnotations` — Options validation with data annotations.
- `Microsoft.Extensions.Resilience` — Resilience pipeline primitives (Polly integration).
- `System.Configuration.ConfigurationManager` — Configuration file access.
- `System.Diagnostics.EventLog` — Windows Event Log read and write.
- `System.Management` — WMI and Windows management instrumentation.
- `System.Security.Cryptography.ProtectedData` — DPAPI data protection.
- `System.ServiceProcess.ServiceController` — Windows Service control.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Microsoft.Windows.CsWin32

- Copyright (c) Microsoft Corporation.
- License: MIT
- Source: https://github.com/microsoft/CsWin32
- Package: https://www.nuget.org/packages/Microsoft.Windows.CsWin32/

The package is referenced as a private build dependency and generates the Windows Firewall COM bindings compiled into IDDS Community. The generator package itself is not included in the application distribution.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## SQLite3 Multiple Ciphers 與 SQLitePCLRaw

- Copyright (c) 2023-2026 Ulrich Telle and contributors
- Copyright (c) 2014-2026 Eric Sink and contributors
- License: MIT
- Source: https://github.com/utelle/SQLite3MultipleCiphers
- Package: https://www.nuget.org/packages/SQLite3MC.PCLRaw.bundle/

`SQLite3MC.PCLRaw.bundle` 提供 SQLite3 Multiple Ciphers 原生資料庫引擎與 SQLitePCLRaw 繫結，供 `Microsoft.Data.Sqlite.Core` 加密 SQLite 主資料庫、日誌及備份。上游套件同時包含各加密演算法來源專案的授權；完整逐檔授權以上游儲存庫的授權文件為準。

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
