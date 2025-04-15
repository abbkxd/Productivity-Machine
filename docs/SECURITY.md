# Security Setup Guide

This document provides detailed instructions for securing your Productive Machine setup on a Raspberry Pi.

## SSH Key-Based Authentication

For secure remote access, SSH key-based authentication should be used instead of passwords.

### Generating SSH Keys

1. On your client machine (e.g., your PC or Mac), open a terminal and run:
   ```
   ssh-keygen -t ed25519 -C "your_email@example.com"
   ```

2. When prompted for a location, accept the default or specify a custom path.

3. Enter a secure passphrase when prompted (highly recommended).

4. Copy the public key to your Raspberry Pi:
   ```
   ssh-copy-id pi@your-raspberry-pi-ip
   ```

### Securing SSH on the Raspberry Pi

1. Edit the SSH configuration:
   ```
   sudo nano /etc/ssh/sshd_config
   ```

2. Ensure the following settings are set:
   ```
   PasswordAuthentication no
   PermitRootLogin no
   Protocol 2
   ```

3. Restart the SSH service:
   ```
   sudo systemctl restart ssh
   ```

## Firewall Configuration

The Uncomplicated Firewall (UFW) is used to restrict network access.

1. Install UFW if not already installed:
   ```
   sudo apt install ufw
   ```

2. Allow only necessary ports:
   ```
   sudo ufw allow ssh        # Port 22
   sudo ufw allow 80/tcp     # HTTP (for Nginx)
   sudo ufw allow 443/tcp    # HTTPS (for Nginx)
   ```

3. Enable UFW:
   ```
   sudo ufw enable
   ```

4. Check status:
   ```
   sudo ufw status
   ```

## Fail2Ban Setup

Fail2Ban helps protect against brute-force attacks.

1. Install Fail2Ban:
   ```
   sudo apt install fail2ban
   ```

2. Create a local configuration file:
   ```
   sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
   ```

3. Edit the configuration:
   ```
   sudo nano /etc/fail2ban/jail.local
   ```

4. Configure SSH protection by ensuring the following settings:
   ```
   [sshd]
   enabled = true
   port = ssh
   filter = sshd
   logpath = /var/log/auth.log
   maxretry = 3
   bantime = 3600  # Ban for 1 hour (adjust as needed)
   ```

5. Start and enable Fail2Ban:
   ```
   sudo systemctl enable fail2ban
   sudo systemctl start fail2ban
   ```

## Nginx as Reverse Proxy with SSL

1. Install Nginx:
   ```
   sudo apt install nginx
   ```

2. Generate a self-signed SSL certificate (or use Let's Encrypt for a proper certificate):
   ```
   sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout /etc/ssl/private/nginx-selfsigned.key -out /etc/ssl/certs/nginx-selfsigned.crt
   ```

3. Create an Nginx configuration:
   ```
   sudo nano /etc/nginx/sites-available/productive-machine
   ```

4. Add the following configuration:
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

5. Enable the site and restart Nginx:
   ```
   sudo ln -s /etc/nginx/sites-available/productive-machine /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl restart nginx
   ```

## Two-Factor Authentication (2FA)

The Productive Machine application includes built-in 2FA using TOTP.

1. After setting up the application, log in to the web interface.
2. Go to your user profile or security settings.
3. Follow the instructions to enable 2FA:
   - Scan the QR code with an authenticator app like Google Authenticator or Authy
   - Enter the verification code to confirm setup
   - Save your backup codes in a secure location

## Backup Encryption with GPG

1. Install GPG:
   ```
   sudo apt install gnupg
   ```

2. Generate a GPG key:
   ```
   gpg --full-generate-key
   ```

3. Follow the prompts to create your key.

4. Export your public key to use for backup encryption:
   ```
   gpg --export --armor your-email@example.com > public-key.asc
   ```

5. Import this key on any machine you want to use to decrypt backups:
   ```
   gpg --import public-key.asc
   ```

6. Update the `GPGRecipient` setting in `appsettings.json` with your email.

## Cloud Backup with Rclone

1. Install Rclone:
   ```
   sudo apt install rclone
   ```

2. Configure Rclone for your cloud provider:
   ```
   rclone config
   ```

3. Follow the interactive prompts to set up your cloud storage (Google Drive, Dropbox, etc.).

4. Create a configuration directory:
   ```
   sudo mkdir -p /etc/rclone
   sudo chmod 755 /etc/rclone
   ```

5. Copy your rclone configuration:
   ```
   sudo cp ~/.config/rclone/rclone.conf /etc/rclone/
   ```

6. Update the backup settings in `appsettings.json` with your rclone remote name.

## Regular System Updates

Set up automatic security updates:

```
sudo apt install unattended-upgrades
sudo dpkg-reconfigure unattended-upgrades
```

## Additional Security Measures

1. **Change default passwords**: Ensure you've changed the default Raspberry Pi password.

2. **Disable unnecessary services**: Turn off any services you don't need.

3. **Keep software updated**: Regularly update all software with:
   ```
   sudo apt update && sudo apt upgrade -y
   ```

4. **Monitor logs**: Regularly check system logs for suspicious activity:
   ```
   sudo journalctl -f
   ```

5. **Consider network isolation**: Consider placing your Raspberry Pi on a separate VLAN if your router supports it. 