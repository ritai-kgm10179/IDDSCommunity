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
1. Run `Setup.exe` and click **"Uninstall"** (displayed when already installed).
2. The installer automatically stops the Windows service, removes created firewall rules, and cleans up shortcuts.

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

### 3.4 🛡️ Safe Networks (Allow List)
Maintain IP addresses and subnets that are never blocked (management hosts, internal gateways, etc.):
- **Add Allowed Address**: Accepts a single IPv4, a single IPv6, an IPv4 CIDR (`192.168.0.0/16`), or an IPv6 CIDR (`fe80::/10`).
- **Automatic Loopback Protection**: Local IPv4 (`127.0.0.1`) and IPv6 (`::1`) are automatically recognized and protected at the service layer; no manual entry is required.
- **Whitelist Collision Exclusion Guard**: Even if an allow-listed IP crosses any agent's attack threshold, the firewall blocking module automatically excludes it, ensuring the management channel is never severed.

### 3.5 ⚙️ Agent Configuration
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

### 3.6 🚨 Lockout Policy
Controls the escalating defensive response after an attack is triggered:
- **Soft Lock**: Once the initial threshold is reached, the offending IP is held in an in-memory lock for a configured duration (e.g. `15 minutes`), during which requests to the affected service from that IP are rejected.
- **Hard Lock**: Once accumulated failures reach the hard-lock threshold, or an attack continues through a soft lock, a Windows Firewall API call creates a physical block rule (the rule name includes the `Blocked by IDDS Community` prefix and belongs to the `IDDS Community` firewall rule group).

### 3.7 📧 SMTP Notifications
Automatically send email alerts when a hard lock or a critical event is triggered:
- **Sending Configuration**: Configure the SMTP server, port, SSL/TLS encryption, sender, and recipient addresses.
- **Test Email**: Click "Send Test Email" to verify the SMTP configuration immediately.
- **Encrypted Configuration Export**: When exporting the configuration package, the SMTP password is protected with **Argon2id key derivation + AES-256-GCM** authenticated encryption so the secret is never exposed in plaintext.

### 3.8 🧹 Database Maintenance and Compaction
Manage SQLite historical logs, space reclamation, and integrity maintenance:
- **Automatic Log Retention**: The background service runs a maintenance pass every 24 hours by default, purging logs older than the configured retention period in batches. The retention period is adjustable and can help support log-retention requirements found in regulations such as PCI DSS; this software makes no official compliance claim, and organizations remain responsible for reviewing applicable requirements themselves.
- **Manual Verified Backup**: Click "Create Verified Backup" to produce a ChaCha20-Poly1305-encrypted, SHA-256-verified SQLite backup file; a separate "Verify Selected Backup" action confirms the file is complete and usable. Backups reuse the DPAPI-protected database key of the local host, so cross-machine disaster recovery requires the corresponding key-preservation procedure.
- **Vacuum / Compact**: Click "Reclaim Database Space" to run `PRAGMA optimize` and `VACUUM`, physically freeing disk space occupied by deleted data (a protective rollback copy is created automatically before the operation runs).
- **Fully Localized Maintenance History**: The history list supports full Traditional Chinese and English i18n translation (e.g. "log retention cleanup", "database space reclamation", "succeeded"), and remains consistent with the underlying audit log event codes.

Local backups are intended for rapid recovery on the local machine and do not replace an off-site disaster recovery plan. Encrypted backups are bound to the DPAPI master key of the current host installation and cannot be restored on a different machine without that key.

---

## 4. Frequently Asked Questions (FAQ)

- **Q: I accidentally blocked my own management host's IP address. What should I do?**
  - **A**: Open the Admin Console, go to "Current Locks", find the target IP, and click "Remove Lock". Afterward, be sure to add that IP or its CIDR subnet to the allow list on the "Safe Networks" page.
- **Q: Why isn't a firewall block rule taking effect?**
  - **A**: Confirm that the `IDDSCommunityProtection` Windows service is running normally, and that the account it runs as has permission to manage the Windows Firewall.
- **Q: How do I safely back up and migrate my configuration?**
  - **A**: In the Admin Console, click "Settings > Export Configuration" to produce an encrypted `.json` package. After installing on the new server, choose "Import Configuration" to restore it in seconds.
