# Productive Machine Setup Guide

This document provides comprehensive instructions for setting up the Productive Machine application on a Raspberry Pi. It covers hardware requirements, software installation, configuration, and first-time setup procedures.

## Table of Contents

1. [Hardware Requirements](#hardware-requirements)
2. [Operating System Setup](#operating-system-setup)
3. [Initial System Configuration](#initial-system-configuration)
4. [Software Dependencies Installation](#software-dependencies-installation)
5. [Application Installation](#application-installation)
6. [Database Setup](#database-setup)
7. [Email Configuration](#email-configuration)
8. [Backup Configuration](#backup-configuration)
9. [Security Configuration](#security-configuration)
10. [Web Server Configuration](#web-server-configuration)
11. [Running the Application](#running-the-application)
12. [First-Time Setup](#first-time-setup)
13. [Troubleshooting](#troubleshooting)

## Hardware Requirements

### Minimum Requirements
- Raspberry Pi 3B+ or newer (Raspberry Pi 4 with 2GB RAM recommended)
- 16GB microSD card (32GB or larger recommended)
- Power supply appropriate for your Raspberry Pi model
- Ethernet cable or Wi-Fi connectivity
- USB keyboard, mouse, and HDMI display (for initial setup only)

### Optional Hardware
- UPS (Uninterruptible Power Supply) for power outage protection
- External USB drive for additional backup storage

## Operating System Setup

1. **Download Raspberry Pi OS**:
   - Visit the [Raspberry Pi website](https://www.raspberrypi.org/software/operating-systems/)
   - Download Raspberry Pi OS Lite (for headless setup) or Raspberry Pi OS with Desktop

2. **Flash the OS**:
   - Use [Raspberry Pi Imager](https://www.raspberrypi.org/software/) to flash the OS to your microSD card
   - Advanced options: Set hostname, enable SSH, configure Wi-Fi, and set locale settings

3. **First Boot**:
   - Insert the microSD card into the Raspberry Pi
   - Connect power, keyboard, monitor, and ethernet (if not using Wi-Fi)
   - Wait for the system to boot

4. **Initial Login**:
   - Login with default credentials (username: `pi`, password: `raspberry`)
   - Change the default password immediately using `passwd` command

## Initial System Configuration

1. **Update the System**:
   ```bash
   sudo apt update
   sudo apt upgrade -y
   ```

2. **Set Hostname** (if not set during OS installation):
   ```bash
   sudo hostnamectl set-hostname productive-machine
   ```

3. **Configure Timezone**:
   ```bash
   sudo dpkg-reconfigure tzdata
   ```

4. **Configure Locale** (if not set during OS installation):
   ```bash
   sudo dpkg-reconfigure locales
   ```

5. **Enable Required Services**:
   ```bash
   sudo raspi-config
   ```
   - Navigate to Interfacing Options
   - Enable SSH (if not already enabled)
   - Enable any other required interfaces

## Software Dependencies Installation

1. **Install .NET SDK**:
   ```bash
   # Download Microsoft signing key and add repository
   wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   rm packages-microsoft-prod.deb

   # Update package lists and install .NET SDK
   sudo apt update
   sudo apt install -y apt-transport-https
   sudo apt install -y dotnet-sdk-7.0
   ```

2. **Install Required Dependencies**:
   ```bash
   sudo apt install -y nginx sqlite3 ufw fail2ban rclone gnupg unzip git curl
   ```

3. **Verify Installations**:
   ```bash
   dotnet --version
   nginx -v
   sqlite3 --version
   ```

## Application Installation

1. **Create Application Directory**:
   ```bash
   sudo mkdir -p /opt/productive-machine
   sudo chown $USER:$USER /opt/productive-machine
   ```

2. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/productive-machine.git /opt/productive-machine
   cd /opt/productive-machine
   ```
   
   Alternatively, download and extract the release package:
   ```bash
   wget https://github.com/yourusername/productive-machine/releases/download/v1.0.0/productive-machine-v1.0.0.zip
   unzip productive-machine-v1.0.0.zip -d /opt/productive-machine
   cd /opt/productive-machine
   ```

3. **Make Scripts Executable**:
   ```bash
   chmod +x ./scripts/*.sh
   ```

4. **Run the Setup Script**:
   ```bash
   ./scripts/setup.sh
   ```

## Database Setup

1. **Create Data Directory**:
   ```bash
   mkdir -p /opt/productive-machine/data
   ```

2. **Initialize the Database**:
   ```bash
   cd /opt/productive-machine
   dotnet ef database update --project src/ProductiveMachine.WebApp
   ```

3. **Verify Database Creation**:
   ```bash
   ls -la /opt/productive-machine/data/productive_machine.db
   ```

## Email Configuration

1. **Edit Configuration File**:
   ```bash
   nano /opt/productive-machine/src/ProductiveMachine.WebApp/appsettings.json
   ```

2. **Configure Email Settings**:
   Update the following section with your SMTP server details:
   ```json
   "Email": {
     "SmtpServer": "smtp.example.com",
     "SmtpPort": 587,
     "SmtpUsername": "your-email@example.com",
     "SmtpPassword": "your-password",
     "SenderEmail": "your-email@example.com",
     "UseSsl": true
   }
   ```

3. **Test Email Configuration**:
   Once the application is running, try sending a test email through the admin interface.

## Backup Configuration

1. **Create Backup Directory**:
   ```bash
   mkdir -p /opt/productive-machine/backups
   ```

2. **Configure rclone** (for cloud backups):
   ```bash
   rclone config
   ```
   Follow the interactive prompts to set up your cloud storage provider (Google Drive, Dropbox, etc.)

3. **Configure GPG** (for encrypted backups):
   ```bash
   gpg --full-generate-key
   ```
   Follow the prompts to create a GPG key pair

4. **Update Backup Settings**:
   Edit the backup section in `appsettings.json`:
   ```json
   "Backup": {
     "Directory": "/opt/productive-machine/backups",
     "DataDirectory": "/opt/productive-machine/data",
     "RcloneConfigPath": "/etc/rclone/rclone.conf",
     "EncryptBackups": true,
     "UploadToCloud": true,
     "CloudProvider": "googledrive",
     "CloudDirectory": "productive_machine_backups",
     "GPGRecipient": "your-email@example.com",
     "FrequencyHours": 24
   }
   ```

5. **Create rclone Configuration Directory**:
   ```bash
   sudo mkdir -p /etc/rclone
   sudo chmod 755 /etc/rclone
   sudo cp ~/.config/rclone/rclone.conf /etc/rclone/
   ```

## Security Configuration

1. **Configure SSH Key Authentication**:
   - From your client machine (laptop/desktop):
     ```bash
     ssh-keygen -t ed25519 -C "your_email@example.com"
     ssh-copy-id pi@raspberry-pi-ip-address
     ```

2. **Disable Password Authentication**:
   ```bash
   sudo nano /etc/ssh/sshd_config
   ```
   Change or add the following lines:
   ```
   PasswordAuthentication no
   PermitRootLogin no
   ```
   Restart SSH:
   ```bash
   sudo systemctl restart ssh
   ```

3. **Configure Firewall**:
   ```bash
   sudo ufw allow ssh
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw --force enable
   ```

4. **Configure Fail2Ban**:
   ```bash
   sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
   sudo nano /etc/fail2ban/jail.local
   ```
   Customize settings as needed, then restart Fail2Ban:
   ```bash
   sudo systemctl restart fail2ban
   ```

## Web Server Configuration

1. **Generate SSL Certificate** (Self-signed for development):
   ```bash
   sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
     -keyout /etc/ssl/private/nginx-selfsigned.key \
     -out /etc/ssl/certs/nginx-selfsigned.crt
   ```

2. **Configure Nginx**:
   ```bash
   sudo nano /etc/nginx/sites-available/productive-machine
   ```
   Add the following configuration:
   ```
   server {
       listen 80;
       server_name your-raspberry-pi-hostname;
       return 301 https://$host$request_uri;
   }

   server {
       listen 443 ssl;
       server_name your-raspberry-pi-hostname;

       ssl_certificate /etc/ssl/certs/nginx-selfsigned.crt;
       ssl_certificate_key /etc/ssl/private/nginx-selfsigned.key;
       ssl_protocols TLSv1.2 TLSv1.3;
       ssl_prefer_server_ciphers on;
       ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
       ssl_session_cache shared:SSL:10m;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

3. **Enable the Site**:
   ```bash
   sudo ln -s /etc/nginx/sites-available/productive-machine /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl restart nginx
   ```

## Running the Application

### Option 1: Run as a Systemd Service (Recommended)

1. **Create a Systemd Service File**:
   ```bash
   sudo cp /opt/productive-machine/scripts/productive-machine.service /etc/systemd/system/
   ```

2. **Edit the Service File if Needed**:
   ```bash
   sudo nano /etc/systemd/system/productive-machine.service
   ```
   Update the paths if you installed to a different location.

3. **Enable and Start the Service**:
   ```bash
   sudo systemctl enable productive-machine
   sudo systemctl start productive-machine
   ```

4. **Check Service Status**:
   ```bash
   sudo systemctl status productive-machine
   ```

### Option 2: Run Manually

```bash
cd /opt/productive-machine
dotnet run --project src/ProductiveMachine.WebApp
```

## First-Time Setup

After the application is running, follow these steps to complete the setup:

1. **Access the Web Interface**:
   - Open a web browser and navigate to `https://your-raspberry-pi-hostname` or `https://your-raspberry-pi-ip`
   - Accept the self-signed certificate warning (in development mode)

2. **First-Time Login**:
   - Log in with the default admin account:
     - Username: `admin@example.com`
     - Password: `Admin123!`
   - You will be prompted to change this password immediately

3. **Configure Two-Factor Authentication**:
   - Navigate to your user profile/security settings
   - Enable 2FA
   - Scan the QR code with an authenticator app (Google Authenticator, Authy, etc.)
   - Enter the verification code to confirm setup
   - Save your backup codes in a secure location

4. **Create Categories**:
   - Set up task categories that match your workflow
   - Assign colors to each category for easy visual identification

5. **Configure Email Settings**:
   - Verify that the email settings in `appsettings.json` are working
   - Send a test email through the admin interface

6. **Set Up First Tasks**:
   - Create some initial to-do items
   - Set up recurring tasks for regular activities
   - Create your first journal entry

7. **Test Backup System**:
   - Run a manual backup
   - Verify that the backup is created successfully
   - If cloud backup is configured, verify that the backup is uploaded

## Troubleshooting

### Application Won't Start

1. **Check Logs**:
   ```bash
   sudo journalctl -u productive-machine.service -n 50
   ```

2. **Check Permissions**:
   ```bash
   sudo chown -R $USER:$USER /opt/productive-machine
   chmod +x /opt/productive-machine/scripts/*.sh
   ```

3. **Verify Database Location**:
   ```bash
   ls -la /opt/productive-machine/data/
   ```

### Cannot Access Web Interface

1. **Check if the application is running**:
   ```bash
   sudo systemctl status productive-machine
   ```

2. **Check Nginx configuration**:
   ```bash
   sudo nginx -t
   ```

3. **Check firewall settings**:
   ```bash
   sudo ufw status
   ```

4. **Check if Nginx is proxying correctly**:
   ```bash
   curl http://localhost
   ```

### Email Issues

1. **Verify SMTP settings** in `appsettings.json`
2. **Check application logs** for email sending errors
3. **Try a different SMTP provider** if issues persist

### Database Issues

1. **Check database file exists**:
   ```bash
   ls -la /opt/productive-machine/data/productive_machine.db
   ```

2. **Check database permissions**:
   ```bash
   sudo chown $USER:$USER /opt/productive-machine/data/productive_machine.db
   ```

3. **Verify connection string** in `appsettings.json`

## Maintenance Tasks

### Regular Updates

1. **Update the application**:
   ```bash
   cd /opt/productive-machine
   git pull
   dotnet build
   sudo systemctl restart productive-machine
   ```

2. **Update the system**:
   ```bash
   sudo apt update
   sudo apt upgrade -y
   ```

### Backup Verification

1. **Check backup logs** regularly
2. **Test restore process** periodically
3. **Verify cloud storage** backups can be accessed

### Security Updates

1. **Regular password rotation**
2. **Update SSL certificates** before expiration
3. **Review firewall rules** periodically 