# Productive Machine

A secure productivity tool built with ASP.NET Core for Raspberry Pi, featuring task management, journaling, recurring schedules, and self-destructing emails.

## Overview

The Productive Machine is designed to run on a Raspberry Pi within your home network. It combines a web-based dashboard (built in ASP.NET Core) with secure SSH access, an SQLite database backend, and automated backup and notification systems. The system prioritizes security with features including public/private key authentication, firewall protection, and two-factor authentication.

## Features

- **Secure Task Management**: Create, organize, and track tasks with status, due dates, and categories
- **Journal System**: Record thoughts and notes with timestamp tracking
- **Recurrence Schedules**: Set up recurring tasks with flexible scheduling options
- **Self-Destructing Emails**: Send reminders that automatically expire to prevent inbox clutter
- **Enhanced Security**: SSH key-based authentication, 2FA, and encrypted backups
- **Automated Backups**: Scheduled encrypted backups to cloud storage

## Tech Stack

- **Backend**: ASP.NET Core (C#)
- **Database**: SQLite 
- **Security**: OpenSSH, UFW, Fail2Ban, TOTP (2FA)
- **Backup**: Rclone, GPG
- **Email**: Postfix/SMTP
- **Scheduling**: Hangfire

## Getting Started

### Prerequisites

- Raspberry Pi with Raspbian or similar Linux distribution
- .NET 7.0 SDK or later
- Git

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/productive-machine.git
   cd productive-machine
   ```

2. Run the setup script to configure security settings:
   ```
   chmod +x ./scripts/setup.sh
   ./scripts/setup.sh
   ```

3. Restore and build the project:
   ```
   dotnet restore
   dotnet build
   ```

4. Initialize the database:
   ```
   dotnet ef database update
   ```

5. Run the application:
   ```
   dotnet run --project src/ProductiveMachine.WebApp
   ```

### Security Setup

For detailed security setup instructions, including:
- SSH key configuration
- Firewall rules
- Fail2Ban configuration
- 2FA setup

See the [Security Setup Guide](docs/SECURITY.md).

## Project Structure

```
/productive-machine
├── /src
│   ├── ProductiveMachine.WebApp     // ASP.NET Core Web Application
│   └── ProductiveMachine.Jobs       // Hangfire jobs and background tasks
├── /data
│   └── productive_machine.db        // SQLite database
├── /scripts
│   ├── backup.sh                    // Backup script using rclone and GPG
│   └── setup.sh                     // Initial setup script
├── README.md
└── LICENSE                          // MIT License
```

## License

This project is licensed under the MIT License - see the LICENSE file for details. 