# IDDS Community - Installer User Guide (Setup Guide)

Welcome to the **IDDS Community** Installer Guide! This document provides complete instructions for system administrators deploying, upgrading, repairing, and uninstalling IDDS Community using Setup.exe.

---

## 1. System Requirements and Prerequisites

- **Supported Operating Systems**:
  - Windows Server 2025, Windows Server 2022, Windows Server 2019, Windows Server 2016
  - Windows 11, Windows 10 (64-bit x64 or ARM64)
- **Privilege Requirements**:
  - Installation, upgrade, and uninstallation require **Local Administrator Rights (Run as Administrator)**. The installer automatically requests UAC elevation.
- **Runtime Environment**:
  - IDDS Community distributions are **Self-Contained**. The package includes an optimized .NET 10 runtime; target servers do not require pre-installed .NET SDKs or runtimes.
- **Default Installation Paths**:
  - Application Binaries: %ProgramFiles%\IDDS Community (typically C:\Program Files\IDDS Community)
  - Operational Data & Database: %ProgramData%\IDDSCommunity (typically C:\ProgramData\IDDSCommunity)

---

## 2. Installation and Lifecycle Management

### 2.1 Fresh Installation

1. Download the appropriate distribution archive (idds-community-3.0.0-win-x64.zip or idds-community-3.0.0-win-arm64.zip) from official GitHub Releases and extract files.
2. Right-click Setup.exe and select **"Run as Administrator"**.
3. Review the license agreement and version information.
4. Select shortcut preferences:
   - **Create Desktop Shortcut**: Deploys a shortcut to Public Desktop (C:\Users\Public\Desktop) accessible to all administrators.
   - **Create Start Menu Shortcut**: Installs shortcuts under the IDDS Community Start Menu folder.
5. Click **"Install"**.
6. The installer executes the following operations:
   - Deploys core service binaries, Admin Console, security agent plugins, and assets to %ProgramFiles%\IDDS Community.
   - Registers and launches the Windows background service: IDDSCommunityProtection.
   - Configures service startup to "Automatic (Delayed Start)" with recovery restart policies.
   - Initializes the DPAPI-encrypted SQLite database (iddscommunity.db).
   - Configures default firewall rule groups and shortcuts.
7. Click "Finish" to complete setup and optionally launch the Admin Console.

### 2.2 Upgrade and Repair

- When executing Setup.exe on a system with an existing installation, the installer detects the deployed version and changes the action button to **"Upgrade"** or **"Reinstall"**.
- During upgrade:
  1. The installer gracefully halts the active IDDSCommunityProtection service.
  2. Updates core executables and agent plugins while preserving user logs, lock records, and safe network configurations.
  3. Executes forward database schema migrations.
  4. Restarts the protection service and verifies service health.

### 2.3 Downgrade Protection

- If an administrator executes an older installer on a system running a newer version, the installer displays a downgrade warning modal.
- Explicit confirmation is required to prevent accidental schema incompatibility.

### 2.4 Uninstallation

1. Trigger uninstallation through any of the following methods:
   - Windows "Settings -> Apps -> Installed apps" or Control Panel "Programs and Features", select "IDDS Community" and click Uninstall.
   - Run the cached installer `%ProgramData%\IDDS Community\Setup.exe` or original setup executable and click **"Uninstall"**.
   - Click the Start Menu shortcut "Uninstall IDDS Community".
   - Execute an unattended silent uninstallation from an elevated command prompt: `.\Setup.exe /uninstall /quiet`.
2. The uninstallation process performs the following cleanup operations:
   - Gracefully halts and removes the IDDSCommunityProtection Windows service.
   - Removes all firewall rules created by the system via native COM APIs (rule group: IDDS Community and associated inbound/outbound rules).
   - Unregisters the uninstallation entry from the Windows Registry (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\IDDS Community`).
   - Removes desktop and Start Menu shortcuts.
   - Deletes `%ProgramFiles%\IDDS Community` program files.
   - Safely cleans up the cached setup executable (`%ProgramData%\IDDS Community\Setup.exe`).
3. **Data Retention**:
   - To protect forensic audit history, the database and logs in `%ProgramData%\IDDSCommunity` are preserved. To completely purge data, remove this directory manually.

---

## 3. Automation and CLI Deployment

The installer supports a full suite of command-line switches for enterprise unattended deployment and orchestration via GPO, Microsoft Intune, SCCM, Ansible, or PowerShell:

### 3.1 Command-Line Switches

| Option | Shorthand | Description |
| :--- | :--- | :--- |
| `/install` | `-i`, `--install` | Requests an installation or upgrade operation (default behavior). |
| `/uninstall` | `-u`, `--uninstall` | Requests an uninstallation operation. |
| `/quiet` | `/silent`, `-q`, `-s`, `--quiet`, `--silent` | Enables unattended silent mode without UI windows or prompts. |
| `/nodesktop` | `-nodesktop`, `--nodesktop` | Skips creating public desktop shortcuts. |
| `/nostartmenu` | `-nostartmenu`, `--nostartmenu` | Skips creating Start Menu shortcuts. |
| `--verify-reinstall` | | CI/CD verification switch: performs uninstall, clean install, and overwrite regression verification. |

### 3.2 Exit Codes

In silent mode (`/quiet`), `Setup.exe` returns structured exit codes indicating operational status:

| Exit Code | Constant / Meaning | Description |
| :--- | :--- | :--- |
| `0` | `SUCCESS` | Operation completed successfully. |
| `3010` | `ERROR_SUCCESS_REBOOT_REQUIRED` | Operation completed successfully, but a system reboot is required to finalize locked files. |
| `2` | `ERROR_FILE_NOT_FOUND / CLEANUP_INCOMPLETE` | Operation completed, but in-use files were scheduled for deferred deletion upon reboot. |
| `1` | `ERROR_FUNCTION_FAILED` | Operation failed; error details are recorded in the diagnostic log. |

### 3.3 Common Automation Examples

```powershell
# 1. Enterprise server unattended silent installation (no desktop shortcut)
.\Setup.exe /install /quiet /nodesktop

# 2. Standard silent installation (desktop & Start Menu shortcuts included)
.\Setup.exe /install /quiet

# 3. Unattended silent uninstallation
.\Setup.exe /uninstall /quiet

# 4. CI/CD automated reinstall verification
.\Setup.exe --verify-reinstall
```

---

## 4. Troubleshooting FAQ

- **Q1: Setup prompts "Access Denied" upon launch?**
  - **Resolution**: Ensure you launch Setup.exe with "Run as Administrator". Installing Windows services and firewall rules requires elevated administrative privileges.
- **Q2: Service fails to start after installation?**
  - **Resolution**: Open Windows Event Viewer (Windows Logs -> Application) and inspect entries from source IDDSCommunity. Verify that security software has not blocked service registration or required communication ports.
- **Q3: Blocked IPs remain inaccessible after uninstallation?**
  - **Resolution**: If the Windows Firewall service was inactive during uninstallation, open "Windows Defender Firewall with Advanced Security" and delete inbound/outbound rules prefixed with Blocked by IDDS Community.
