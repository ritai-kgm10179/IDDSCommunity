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

---

## 4. Maintenance and Backup Responsibilities

Local backups serve for rapid recovery on the local machine and do not replace off-site disaster recovery plans. Encrypted backups inherit the DPAPI master key of the current host installation.
