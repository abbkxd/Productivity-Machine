# Productive Machine Project

This project is a productivity tool built with ASP.NET Core running on a Raspberry Pi. It features a secure task/journal management system with to-do lists, recurrence schedules, self-destructing emails, and robust security measures. All chosen resources are open source and focused on security.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack & Open Source Tools](#tech-stack--open-source-tools)
- [Architecture Overview](#architecture-overview)
- [Features & Functionality](#features--functionality)
  - [Security](#security)
  - [Backup System](#backup-system)
  - [Self-Destructing Emails](#self-destructing-emails)
  - [To-Do / Journal System](#to-do--journal-system)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Future Enhancements](#future-enhancements)

---

## Overview

The Productive Machine is designed to run on a Raspberry Pi within your home network. It combines a web-based dashboard (built in ASP.NET Core) with secure SSH access, an SQL database backend, and automated backup and notification systems. The system uses best practices in security, including public/private key authentication, firewall protection, and two-factor authentication.

---

## Tech Stack & Open Source Tools

### Hardware & Platform
- **Raspberry Pi** – Single-board computer running a secure Linux distribution (e.g., Raspbian).
  
### Programming Framework & Language
- **ASP.NET Core (C#)** – The web application framework for building the dashboard and API endpoints.

### Database
- **SQLite / SQL Server Express (Open Source Editions)** – Lightweight relational database for storing user tasks, journals, logs, and settings.

### Security & Access
- **OpenSSH** – Enables key-based SSH access; configure SSH to disable password authentication.
- **Termius / Blink Shell / Working Copy (iOS)** – For managing and storing your private key securely on your iPhone.
- **Fail2Ban** – Protects against brute-force attacks by dynamically blocking suspicious IP addresses.
- **UFW (Uncomplicated Firewall)** – Simple yet powerful firewall for configuring network access (allowing only ports 22 for SSH and the ASP.NET application port).
- **Nginx** – Acts as a reverse proxy and enables SSL/TLS encryption (using self-signed or Let’s Encrypt certificates) to secure external access.
- **TOTP (Time-based One-Time Password)** – Implement using open source libraries (e.g., [OATH.NET](https://github.com/jamesmontemagno/OATH.NET)) to provide 2FA support for the web dashboard.

### Backup
- **Rclone** – Command line tool for syncing files with cloud storage (Google Drive).
- **GPG / rclone crypt** – For encrypting backup data.
- **Cron** – Schedule backups to run automatically (e.g., at midnight).

### Email & Notifications
- **Postfix (or another open source SMTP server)** – For sending emails.
- **SMTP Client Libraries in .NET** – To manage outgoing self-destruct email notifications.
- **GUID-based URLs and Timestamps** – To create time-limited access links for email content.
- **Pushover (or an open source alternative)** – For local push notifications from your mobile if desired.

### Scheduling & Task Management
- **Hangfire** – Open source job scheduler for ASP.NET Core to handle recurring tasks, notifications, and to-do list recurrences.

---

## Architecture Overview

        [ iPhone ]
            │
            │  (Secure SSH via Key-Based Authentication)
            │
    -----------------------
    |  Raspberry Pi     |  
    |   (Localhost/LAN) |
    |                   |
    | ASP.NET Core Web  |    ─── Secure API & Web Dashboard
    |    Application    |  --> SQL Database (SQLite/SQL Server Express)
    |                   |  --> Backup Scripts (Cron + Rclone)
    -----------------------
            │
       [ Nginx Reverse Proxy ]
            │ (SSL/TLS)
            │
     (Firewall Controlled via UFW)
            │
      Internet (if needed externally)


- **Raspberry Pi** hosts the ASP.NET Core application, SQL database, and backup system.
- **SSH Access** is secured via OpenSSH using public/private keys.
- **Firewall (UFW)** and **Fail2Ban** ensure network security.
- **Nginx** provides reverse proxy and SSL/TLS termination.

---

## Features & Functionality

### Security

- **SSH Access via Public/Private Key**  
  - Tool: *OpenSSH*  
  - **Configuration:**  
    - Disable password-based login by setting `PasswordAuthentication no` and `PermitRootLogin no` in `/etc/ssh/sshd_config`.  
- **Key Storage on iPhone**  
  - Tools: *Termius, Blink Shell, or Working Copy*  
- **Additional Protections**  
  - **Fail2Ban:** Automatically bans IPs attempting brute-force access.  
  - **UFW:** Strict firewall rules allowing only necessary ports (e.g., port 22 for SSH, application port).  
  - **Reverse Proxy with Nginx:** Manages external access and enables SSL/TLS encryption.  
  - **2FA:** Use TOTP-based authentication (via open source libraries) to secure the web dashboard.

---

### Backup System

- **Tool:** *Rclone*  
- **Cloud Storage:** *Google Drive* (using open source rclone)  
- **Encryption:** Use *rclone crypt* or *GPG* to encrypt backups before upload.  
- **Scheduling:** Automate daily backups via *Cron jobs*.  
- **Monitoring:** Log backup status and send failure notifications via email.

---

### Self-Destructing Emails

- **Email Service:** *Postfix* (configured as an open source SMTP server)  
- **Email Content:**  
  - Embed GUID-based, time-limited access links (valid for 2 days).  
  - Store email timestamps in the database and purge expired messages.
- **Use Case:**  
  - Sending reminders (similar to a To-Do list app) that self-destruct, helping prevent inbox bloat.

---

### To-Do / Journal System

#### Core Task Features
- **Task Recurrence:**  
  - Store recurrence in the SQL database.  
  - Use *Hangfire* to schedule and regenerate repeating tasks (daily, weekly, or custom).
- **Task Status:**  
  - Manage states such as "Not Started," "In Progress," and "Completed".
- **Rescheduling:**  
  - Provide functionality to reschedule incomplete tasks.
- **Completion Tracking:**  
  - Mark tasks with date and time upon completion.
- **Tagging & Categorization:**  
  - Organize tasks by categories (e.g., Home, Work, Personal).

#### Journaling Features
- **Journal Entries:**  
  - Allow users to write small journals.
  - Optionally, support additional content types (text, optionally images later).

#### Notifications
- **Push Notifications:**  
  - Use a tool like *Pushover* or other open source notification systems for sending reminders directly to your phone.

#### Analytics Dashboard
- **Metrics:**  
  - Visualize task completion rates and other activity statistics (daily/weekly/monthly).

---

## Project Structure

```plaintext
/productive-machine
├── /src
│   ├── ProductiveMachine.WebApp      // ASP.NET Core Web Application
│   │    ├── Controllers
│   │    ├── Models
│   │    ├── Views
│   │    └── Services                // Email, Notifications, Task Management, etc.
│   └── ProductiveMachine.Jobs        // Hangfire jobs and background tasks
├── /data
│   └── productive_machine.db         // SQLite DB (or SQL Server Express files)
├── /scripts
│   ├── backup.sh                     // Backup script using rclone and GPG
│   └── setup.sh                      // Initial setup script (firewall, fail2ban, etc.)
├── README.md
└── LICENSE                           // Open source license (e.g., MIT or Apache 2.0)


---
