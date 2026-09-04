# IDDS Community - Installation and User Guide

Welcome to **IDDS Community**! This document provides a comprehensive guide for installation, Admin Console interface operations, SIEM search filtering, security agent configuration, and safe network management.

---

## 1. System Architecture

IDDS Community is a high-performance Windows host-level intrusion detection and active defense system built on .NET 10, comprising three core components:

1. **IDDS Community Protection Service (`IDDSCommunity.IntrusionDetection.Service.exe`)**:
   - Windows background service responsible for event subscriptions, log monitoring, brute-force attack detection, and automated Windows Firewall blocking.
2. **IDDS Community Admin Console (`IDDSCommunity.IntrusionDetection.Admin.exe`)**:
   - GUI administration console providing real-time dashboard monitoring, SIEM-grade log search, safe network list management, and security agent tuning.
3. **Setup Installer (`Setup.exe`)**:
   - Standalone integrated installer/upgrade/repair/uninstallation tool supporting automatic version identification and all-user shortcut management.

---

## 2. Installation, Upgrade, and Uninstallation

### 2.1 Fresh Installation
1. Run `Setup.exe` with **Administrator Rights (Run as Administrator)**.
2. Select desired shortcut options:
   - `[x] Create desktop shortcut` (automatically written to Public Desktop `C:\Users\Public\Desktop`)
   - `[x] Create Start Menu shortcut`
3. Click **"Install"**. The installer will deploy to `C:\Program Files\IDDS Community` and start the background service.

### 2.2 Upgrade and Repair
- When executing a newer version of `Setup.exe`, the installer automatically detects existing installations:
  - **Upgrade / Reinstall**: Click **"Reinstall"** or **"Upgrade"** to seamlessly update service and agent assets.
  - **Downgrade Warning**: Attempting to install an older version prompts a confirmation dialog to prevent accidental downgrades.

### 2.3 Uninstallation
1. Uninstall via Windows "Settings -> Apps -> Installed apps", Control Panel "Programs and Features", the Start Menu shortcut, or by launching `Setup.exe` and clicking **"Uninstall"**.
2. The uninstallation process automatically stops the Windows service, cleans up firewall rules, unregisters Windows Registry entries, and removes shortcuts.

### 2.4 Automation and Silent Deployment (Silent Automation CLI)
- The installer provides comprehensive command-line argument support:
  - Silent install: `.\Setup.exe /install /quiet` (with optional `/nodesktop` or `/nostartmenu`)
  - Silent uninstall: `.\Setup.exe /uninstall /quiet`
- For the full parameter reference, exit codes, and deployment patterns, refer to the [Setup Guide (HTML)](docs/SETUP-GUIDE.en-US.html) and [Markdown Setup Guide](docs/SETUP-GUIDE.en-US.md).

---

## 3. Administration Console Guide

Launch **IDDS Community Admin Console (`IDDSCommunity.IntrusionDetection.Admin.exe`)**. The navigation menu on the left includes 8 core panels:

### 3.1 📊 Dashboard
- **Service Status**: Displays current service state (Running / Stopped). Click "Start Service" or "Stop Service" as needed.
- **Threat Visualizations**: Bar and pie charts depicting attack attempts and lock counts per security agent.
- **Protection Metrics**: Summary of active security agents, soft locks, hard locks, and total blocked attacks.

### 3.2 🔎 Security Log & SIEM Search
Provides high-performance SIEM-grade attack log queries and multi-dimensional filtering:
- **CIDR Subnet Search**:
  - Enter a single IP address (e.g. `192.168.1.50`) or an **IPv4 CIDR subnet (e.g. `192.168.1.0/24`)** to parse all matching attack logs.
- **Composite Event Status Filtering**:
  - Filter by event types: `[x] Intrusion Attempt`, `[x] Soft Lock`, `[x] Hard Lock`, `[x] System`.
- **Security Agent Filtering**:
  - Filter by "All Security Agents" or specific agents (e.g. `RDP` or `OpenSSH`).
- **250ms Debounced Input & Double Buffering**:
  - Search box incorporates a 250ms debounce delay; DataGrid utilizes double-buffering for flicker-free scrolling.

### 3.3 🔒 Current Locks & Manual Relief
Manage source IP addresses currently locked out due to repeated failed logins:
- **View Locked IPs**: Lists blocked addresses, the triggering agent, lock status (Soft Lock / Hard Lock), remaining lock duration, and the time the lock was triggered.
- **Manual Unlock (Remove Lock)**:
  - Select an IP and click "Remove Lock" to immediately clear the lock and remove the address from the Windows Firewall rule.
- **Promote to Hard Lock**: Select a soft-locked entry to manually promote it to an enforced firewall hard lock.

### 3.4 📋 System Operations & Audit Log
Provides administrators with complete forensic evidence and operational visibility for internal defense pipelines, external downloads, and maintenance operations:
- **External Data Download Auditing**:
  - **Threat Intelligence Feeds (`ThreatFeed.Download`)**: Tracks IPsum, AbuseIPDB, Spamhaus DROP downloads, bogon-filtered entries, whitelist-skipped IPs, and network errors.
  - **Team Cymru Fullbogons Dynamic Prefixes (`Bogon.Update`)**: Tracks IPv4 and IPv6 bogon prefix list downloads and update counts.
  - **GeoIP Database Updates (`GeoIp.Update`)**: Tracks MaxMind / DB-IP downloads, loaded prefix counts, country totals, and hot-swap events.
  - **Dynamic DNS FQDN Resolution (`DynamicDns.Resolve`)**: Tracks DNS query results and resolved IPs for whitelisted domain names.
  - **Cluster Threat Intelligence Sync (`Cluster.Sync`)**: Tracks threat items pushed to and pulled from the Threat Hub.
- **Maintenance & Defensive Actions**:
  - **Inbound Allow Rules Lifecycle (`Firewall.RuleAdd` / `Firewall.RuleRemove`)**: Tracks the declarative reconciliation and lifecycle management of Windows Firewall inbound allow rules for internal listening endpoints (Self-Service Portal, Management API, Threat Hub, Honeypot Decoys); automatically aligns on startup, port changes, and shutdown.
  - Audits probation transitions (`Firewall.Probation`), firewall unlocking (`Firewall.Unlock`), automated database maintenance (`Database.Maintenance`), and service runtime events (`Runtime.Start` / `Runtime.Stop`).
- **Multi-Dimensional Filtering & CSV Export**:
  - Filter by event category, outcome (Succeeded / Failed), and free-text search keywords.
  - Export audit logs to standard CSV format for compliance, incident response, and regulatory audits.

### 3.5 🛡️ Safe Networks (Allow List)
Maintain IP addresses and subnets that are never blocked (management hosts, internal gateways, etc.):
- **Add Allowed Address**: Accepts a single IPv4, a single IPv6, an IPv4 CIDR (`192.168.0.0/16`), or an IPv6 CIDR (`fe80::/10`).
- **Automatic Loopback Protection**: Local IPv4 (`127.0.0.1`) and IPv6 (`::1`) are automatically recognized and protected at the service layer; no manual entry is required.
- **Whitelist Collision Exclusion Guard**: Even if an allow-listed IP crosses any agent's attack threshold, the firewall blocking module automatically excludes it, ensuring the management channel is never severed.

### 3.6 ⚙️ Agent Configuration
Configure failure thresholds and the sliding detection window per service:
- **Supported Agent List**:
  - `Windows Network Logon` (SMB/network logon Event 4625)
  - `Remote Desktop` (RDP logon failures)
  - `Windows OpenSSH` (SSH service)
  - `IIS Authentication` & `Web Security` (W3C 401 & HTTP attacks)
  - `Microsoft SQL Server` / `MySQL` / `PostgreSQL` (database connection failures)
  - `Mail Server` (POP3 / IMAP / SMTP authentication failures)
  - `Generic FTP` & `FileZilla Server` (FTP authentication failures)
  - `NPS / RADIUS Server` & `Windows DNS Server` & `Technitium DNS Security` & `FileMaker Server`
- **Threshold Tuning**:
  - Independently configure each agent's "failure threshold" (e.g. `5 attempts`) and "detection window" (e.g. `300 seconds`).

### 3.7 🚨 Lockout Policy
Controls the escalating defensive response after an attack is triggered:
- **Soft Lock**: Once the initial threshold is reached, the offending IP is held in an in-memory lock for a configured duration (e.g. `15 minutes`), during which requests to the affected service from that IP are rejected.
- **Hard Lock**: Once accumulated failures reach the hard-lock threshold, or an attack continues through a soft lock, a Windows Firewall API call creates a physical block rule (the rule name includes the `Blocked by IDDS Community` prefix and belongs to the `IDDS Community` firewall rule group).

### 3.8 📧 SMTP Notifications
Automatically send email alerts when a hard lock or a critical event is triggered:
- **Sending Configuration**: Configure the SMTP server, port, SSL/TLS encryption, sender, and recipient addresses.
- **Test Email**: Click "Send Test Email" to verify the SMTP configuration immediately.
- **Encrypted Configuration Export**: When exporting the configuration package, the SMTP password is protected with **Argon2id key derivation + AES-256-GCM** authenticated encryption so the secret is never exposed in plaintext.

### 3.9 🧹 Database Maintenance and Compaction
Manage SQLite historical logs, space reclamation, and integrity maintenance:
- **Automatic Log Retention**: The background service runs a maintenance pass every 24 hours by default, purging logs older than the configured retention period in batches. The retention period is adjustable and can help support log-retention requirements found in regulations such as PCI DSS; this software makes no official compliance claim, and organizations remain responsible for reviewing applicable requirements themselves.
- **Manual Verified Backup**: Click "Create Verified Backup" to produce a ChaCha20-Poly1305-encrypted, SHA-256-verified SQLite backup file; a separate "Verify Selected Backup" action confirms the file is complete and usable. Backups reuse the DPAPI-protected database key of the local host, so cross-machine disaster recovery requires the corresponding key-preservation procedure.
- **Vacuum / Compact**: Click "Reclaim Database Space" to run `PRAGMA optimize` and `VACUUM`, physically freeing disk space occupied by deleted data (a protective rollback copy is created automatically before the operation runs).
- **Fully Localized Maintenance History**: The history list supports full Traditional Chinese and English i18n translation (e.g. "log retention cleanup", "database space reclamation", "succeeded"), and remains consistent with the underlying audit log event codes.

Local backups are intended for rapid recovery on the local machine and do not replace an off-site disaster recovery plan. Encrypted backups are bound to the DPAPI master key of the current host installation and cannot be restored on a different machine without that key.

### 3.10 🌐 Threat Intelligence & Distributed Cluster Defense
- **Distributed Cluster Defense Topology (Edge / Hub)**:
  - `Standalone`: Single-host independent defense and threat subscription without cluster synchronization.
  - `EdgeNode`: **Requires specifying the "Threat Hub Endpoint URL"** (e.g. `https://hub.example.com:8443` or multiple failover endpoints separated by commas/semicolons) and Cluster API Key; periodically synchronizes high-confidence global threat lists and pushes local hard-lock events to the Hub.
  - `ThreatHub`: **Does NOT require specifying an endpoint URL (ignored if provided)**; only requires configuring the listening "Threat Hub Port" (default TCP 8443) and Cluster API Key; centrally fetches external feeds and broadcasts intelligence to connected edge nodes.
- **Intelligent Probation & One-Strike Relock**:
  - Automatically transitions permanent hard locks with no malicious activity after a configurable period (default 90 days) to a probation observation status, releasing them from the Windows Firewall to prevent stale IP reuse issues from telecom dynamic pools.
  - If an IP under probation triggers any violation again (1 attempt), it is immediately escalated back to a permanent hard lock without waiting for soft lock accumulation.
- **Automated External Threat Feeds Subscription**:
  - Supports automated subscription to open-source IPsum (levels 1-8), AbuseIPDB Blacklists (with API key and confidence threshold), and custom text feed URLs.
  - Feeds enforce TTL expiration (default 7 days) to prevent firewall rule bloat.
- **Dual-Layer Bogon Guardrails & Dynamic DNS Resolver**:
  - Enforces static RFC 1918 private IP hard-filtering alongside Team Cymru Fullbogons IPv4/IPv6 dynamic prefix synchronization to prevent accidental internal lockouts.
  - Safe Networks allow list supports FQDN hostnames (e.g. `office.ddns.net`) with background dynamic DNS resolution.

### 3.11 💬 Multi-Channel Webhook Notifications
Enables real-time push alerts to enterprise messaging platforms and automated SOC pipelines:
- **Supported Platforms**: Microsoft Teams (Adaptive Cards 1.6), Slack (Block Kit), Discord (Rich Embed), Telegram (Bot API `sendMessage`), Generic JSON (RESTful Webhook).
- **Granular Event Triggers**: Independently trigger on Soft Lock, Hard Lock, and Unlock events.
- **Connectivity Testing**: Provides a "Test Webhook" button in "Settings -> Notifications" for instant endpoint verification.

### 3.12 🍯 Honeypot Decoy Agent
Active deception deployed on unused ports (default TCP 23 Telnet, 2222 alternate SSH, 33890 alternate RDP) to catch threat actors early:
- **Zero-Tolerance Hard Lock**: Any unsolicited TCP connection attempt to a decoy port triggers an immediate permanent firewall block.
- **Silent Drop (No Banner)**: Immediately terminates connection without returning service banners or software identification.
- **Whitelist Integration**: Probing sources are verified against BogonIpFilter and Safe Networks allow list.

### 3.13 📊 OASIS STIX 2.1 Threat Sharing & ISO/IEC 27001:2022 Reports
- **OASIS STIX 2.1 JSON Export**:
  - Exports local and cluster threat intelligence as standard STIX 2.1 JSON Bundles (`identity`, `indicator`, `report` SDOs) for integration with SIEM, MISP, OpenCTI, and SOAR systems.
- **ISO/IEC 27001:2022 Annex A Audit Reports**:
  - Built-in compliance report engine evaluating A.5.7 (Threat Intelligence), A.8.7 (Malware Protection / Active Defense), A.8.15 (Logging), A.8.16 (Monitoring Activities), A.8.20 (Network Security), and A.8.24 (Use of Cryptography) into executive HTML reports.

### 3.14 🗺️ GeoIP Country Tagging & Geofencing
- **High-Performance GeoIP Lookup**: Resolves IPv4/IPv6 addresses to ISO 3166-1 country codes and names supporting both CIDR and IP range CSV formats.
- **Automated Feed Updates & Local Offline Caching**: Automatically downloads and refreshes GeoIP databases from remote URLs or local CSV files with fallback persistence in `%ProgramData%\IDDSCommunity\`.
- **Active Country-Based Geo-blocking**: Blocks inbound connections originating from designated country codes (e.g. CN, RU, KP) with immediate one-strike permanent hard lock escalation upon attack detection.

### 3.15 📡 Traditional SOC / SIEM Integration (Syslog & CEF)
- **Standard Format Support**:
  - **RFC 5424**: Modern structured Syslog with enterprise PRI, Timestamp, and Structured-Data.
  - **RFC 3164**: Traditional BSD Syslog for legacy collectors.
  - **ArcSight CEF (Common Event Format)**: Industry-standard security format for Splunk, IBM QRadar, Micro Focus ArcSight.
- **Transport Protocols**: Supports UDP, TCP, and TLS encrypted transmission with built-in test buttons.

### 3.16 📈 Modern Observability (Prometheus & Grafana)
- **Built-in Prometheus Metrics**:
  - Provides OpenMetrics / Prometheus standard `/metrics` endpoint (`idds_active_firewall_blocks`, `idds_uptime_seconds`, `idds_probation_ips_total`).
  - Provides JSON `/healthz` endpoint for uptime monitoring.
- **Custom Binding & Scrape Allow List**: Configurable listen IP address and monitoring CIDR subnet filtering.
- **Grafana Dashboard Template**: Ready-to-import dashboard JSON located at [`assets/dashboards/idds-grafana-dashboard.json`](file:///d:/Dev/Project/Application/IDDSCommunity/assets/dashboards/idds-grafana-dashboard.json).

### 3.17 💻 Official Automation Module (PowerShell 7+)
Located at [`tools/IDDSCommunity.PowerShell/`](file:///d:/Dev/Project/Application/IDDSCommunity/tools/IDDSCommunity.PowerShell/):
- `Get-IddsStatus`: Query service status and database state.
- `Get-IddsBlockedIp`: List all currently blocked IP addresses.
- `Get-IddsSafeNetwork` / `Add-IddsSafeNetwork` / `Remove-IddsSafeNetwork`: Manage allow lists.
- `Export-IddsStixBundle`: Export STIX 2.1 threat intelligence bundles via CLI.
- `Export-IddsIso27001Report`: Generate ISO 27001 compliance audit reports via CLI.
- `Test-IddsNotification`: Batch test notification endpoints.

### 3.18 🔑 Self-Service TOTP Unblock Portal
Dedicated lightweight web portal on a separate port (default TCP 8088) allowing legitimate administrators or users to unblock themselves:
- **TOTP Two-Factor Authentication (RFC 6238)**: Compatible with Google Authenticator, Microsoft Authenticator, and standard TOTP apps.
- **Instant Automatic Relief**: Instantly removes the user's IP from the Windows Firewall upon successful code verification.

### 3.19 ☁️ Cloud Perimeter Auto-Sync (AWS, Azure, Cloudflare)
- **Dynamic Official IP Range Ingestion**: Automatically fetches and parses published JSON IP range lists from AWS, Microsoft Azure, and Cloudflare.
- **Automatic Allow List Merging**: Automatically protects reverse proxies and CDN nodes from false-positive blockages.

### 3.20 🎭 Honey Accounts & SOAR Script Automation
- **Honey Accounts (Decoy Logins)**: Configures decoy account names (e.g. `admin`, `root`, `test`, `guest`, `superadmin`). Any authentication attempt using these accounts triggers an immediate One-Strike Hard Lock.
- **SOAR Script Execution**: Executes custom PowerShell or Batch scripts upon critical security events with event parameters for incident workflow orchestration.

### 3.21 🔌 RESTful Management API
Secure lightweight HTTP/HTTPS REST API server (default TCP 8444) protected by API Keys and Bearer Tokens:
- `GET /api/v1/status`: Query service operational status and security metrics.
- `GET /api/v1/locks`: Retrieve active locked IP list.
- `POST /api/v1/locks/release`: Instantly unblock a specified IP.
- `POST /api/v1/locks/block`: Manually enforce a permanent hard block on a malicious IP.
- `GET /api/v1/safenetworks` / `POST /api/v1/safenetworks`: Manage safe network allow lists.

### 3.22 📋 CIS Windows Server Benchmark & Forensics
- **Five Security Principles Deep Evaluation**: Scans Account Policies, Network Policies, Windows Firewall Configurations, Audit Policies, and Application Security.
- **Instant Score & Remediation Advice**: One-click benchmark scan calculating compliance percentage with detailed remediation guidelines for failed checks.
- **Forensic Report Export**: Exports compliance audit findings to JSON forensic evidence files.

---

## 4. Frequently Asked Questions (FAQ)

- **Q: I accidentally blocked my own management host's IP address. What should I do?**
  - **A**: Open the Admin Console, go to "Current Locks", find the target IP, and click "Remove Lock". Afterward, be sure to add that IP or its CIDR subnet to the allow list on the "Safe Networks" page. If the TOTP Unblock Portal is enabled, you can also unblock yourself directly via your mobile authenticator.
- **Q: Why isn't a firewall block rule taking effect?**
  - **A**: Confirm that the `IDDSCommunityProtection` Windows service is running normally, and that the account it runs as has permission to manage the Windows Firewall.
- **Q: When a node is configured as a Threat Hub, do I need to fill in the "Threat Hub Endpoint URL"?**
  - **A**: No. The Threat Hub functions as the server listener and only requires configuring the listening port (e.g. 8443) and Cluster API Key for edge nodes to connect. Only Edge Nodes need to provide the Threat Hub Endpoint URL. If an endpoint URL is entered on a Threat Hub, it is safely ignored and will cause no errors.
- **Q: How do I safely back up and migrate my configuration?**
  - **A**: In the Admin Console, click "Settings > Export Configuration" to produce an encrypted `.json` package. After installing on the new server, choose "Import Configuration" to restore it in seconds.
