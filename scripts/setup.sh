#!/bin/bash

# Productive Machine Setup Script
# This script sets up the Raspberry Pi environment for running the Productive Machine app

echo "Setting up Productive Machine environment..."

# Update system
echo "Updating system packages..."
sudo apt update
sudo apt upgrade -y

# Install required dependencies
echo "Installing required dependencies..."
sudo apt install -y \
    apt-transport-https \
    gnupg \
    curl \
    ufw \
    fail2ban \
    nginx \
    sqlite3 \
    rclone \
    gpg

# Install .NET SDK
echo "Installing .NET SDK..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 7.0

# Add .NET to PATH
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> ~/.bashrc
source ~/.bashrc

# Configure firewall
echo "Configuring firewall..."
sudo ufw allow ssh
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw --force enable

# Configure fail2ban
echo "Configuring fail2ban..."
sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
sudo systemctl enable fail2ban
sudo systemctl start fail2ban

# Configure Nginx as reverse proxy
echo "Configuring Nginx..."
cat > /tmp/productive-machine.conf << 'EOL'
server {
    listen 80;
    server_name _;
    
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
EOL

sudo mv /tmp/productive-machine.conf /etc/nginx/sites-available/
sudo ln -sf /etc/nginx/sites-available/productive-machine.conf /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl restart nginx

# Create data directory
echo "Creating data directory..."
mkdir -p ../data
mkdir -p ../backups

# Configure SSH for key-based authentication
echo "Configuring SSH for key-based authentication..."
sudo sed -i 's/#PasswordAuthentication yes/PasswordAuthentication no/' /etc/ssh/sshd_config
sudo sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin no/' /etc/ssh/sshd_config
sudo systemctl restart ssh

# Create rclone config directory
sudo mkdir -p /etc/rclone
sudo chmod 755 /etc/rclone

echo "Setup completed successfully!"
echo "To finalize the setup:"
echo "1. Configure your public key authentication for SSH"
echo "2. Set up rclone for cloud backups: run 'rclone config'"
echo "3. Configure the application settings in appsettings.json"
echo "4. Run database migrations with 'dotnet ef database update'" 