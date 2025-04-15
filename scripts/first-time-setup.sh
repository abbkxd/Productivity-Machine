#!/bin/bash

# Productive Machine First-Time Setup Script
# This script assists with the first-time setup of the Productive Machine application

echo "Starting Productive Machine first-time setup..."

# Default installation directory
INSTALL_DIR="/opt/productive-machine"
DATA_DIR="$INSTALL_DIR/data"
BACKUP_DIR="$INSTALL_DIR/backups"
DEFAULT_DB_PATH="$DATA_DIR/productive_machine.db"

# Create necessary directories
echo "Creating application directories..."
sudo mkdir -p "$INSTALL_DIR"
sudo mkdir -p "$DATA_DIR"
sudo mkdir -p "$BACKUP_DIR"

# Set permissions
echo "Setting permissions..."
sudo chown -R $USER:$USER "$INSTALL_DIR"

# Check if repository already exists
if [ -d "$INSTALL_DIR/.git" ]; then
    echo "Repository already exists. Pulling latest changes..."
    cd "$INSTALL_DIR"
    git pull
else
    # Clone the repository
    echo "Cloning repository..."
    git clone https://github.com/yourusername/productive-machine.git "$INSTALL_DIR"
    cd "$INSTALL_DIR"
fi

# Make scripts executable
echo "Making scripts executable..."
chmod +x ./scripts/*.sh

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build the application
echo "Building the application..."
dotnet build

# Initialize the database if it doesn't exist
if [ ! -f "$DEFAULT_DB_PATH" ]; then
    echo "Initializing the database..."
    dotnet ef database update --project src/ProductiveMachine.WebApp
else
    echo "Database already exists at $DEFAULT_DB_PATH"
fi

# Configure application settings
echo "Configuring application settings..."

# Generate a random string for JWT token
JWT_SECRET=$(openssl rand -base64 32)

# Function to update appsettings.json
update_appsettings() {
    # Backup original settings
    if [ -f "$INSTALL_DIR/src/ProductiveMachine.WebApp/appsettings.json" ]; then
        cp "$INSTALL_DIR/src/ProductiveMachine.WebApp/appsettings.json" "$INSTALL_DIR/src/ProductiveMachine.WebApp/appsettings.json.bak"
    fi

    # Get user inputs
    read -p "Enter SMTP server (e.g., smtp.gmail.com): " SMTP_SERVER
    read -p "Enter SMTP port (e.g., 587): " SMTP_PORT
    read -p "Enter SMTP username (email): " SMTP_USERNAME
    read -s -p "Enter SMTP password: " SMTP_PASSWORD
    echo
    read -p "Enable cloud backup? (y/n): " ENABLE_CLOUD
    
    if [[ "$ENABLE_CLOUD" == "y" ]]; then
        read -p "Enter cloud provider (e.g., googledrive): " CLOUD_PROVIDER
        read -p "Enter cloud directory: " CLOUD_DIR
    else
        CLOUD_PROVIDER="googledrive"
        CLOUD_DIR="productive_machine_backups"
    fi
    
    read -p "Enter GPG recipient email (for encrypted backups): " GPG_EMAIL

    # Create new appsettings.json
    cat > "$INSTALL_DIR/src/ProductiveMachine.WebApp/appsettings.json" << EOL
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=$DEFAULT_DB_PATH"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApplicationUrl": "https://$(hostname)",
  "JwtSettings": {
    "Secret": "$JWT_SECRET",
    "ExpiryHours": 24,
    "Issuer": "ProductiveMachine",
    "Audience": "ProductiveMachineUsers"
  },
  "Email": {
    "SmtpServer": "$SMTP_SERVER",
    "SmtpPort": $SMTP_PORT,
    "SmtpUsername": "$SMTP_USERNAME",
    "SmtpPassword": "$SMTP_PASSWORD",
    "SenderEmail": "$SMTP_USERNAME",
    "UseSsl": true
  },
  "Backup": {
    "Directory": "$BACKUP_DIR",
    "DataDirectory": "$DATA_DIR",
    "RcloneConfigPath": "/etc/rclone/rclone.conf",
    "EncryptBackups": true,
    "UploadToCloud": $([ "$ENABLE_CLOUD" == "y" ] && echo "true" || echo "false"),
    "CloudProvider": "$CLOUD_PROVIDER",
    "CloudDirectory": "$CLOUD_DIR",
    "GPGRecipient": "$GPG_EMAIL",
    "FrequencyHours": 24
  }
}
EOL

    echo "Application settings configured."
}

# Ask if the user wants to configure settings now
read -p "Do you want to configure application settings now? (y/n): " CONFIGURE_SETTINGS
if [[ "$CONFIGURE_SETTINGS" == "y" ]]; then
    update_appsettings
else
    echo "Skipping application settings configuration. Please edit appsettings.json manually."
fi

# Set up systemd service
echo "Setting up systemd service..."
sudo cp "$INSTALL_DIR/scripts/productive-machine.service" /etc/systemd/system/
sudo sed -i "s|/home/pi/productive-machine|$INSTALL_DIR|g" /etc/systemd/system/productive-machine.service

# Ask if user wants to enable and start the service
read -p "Do you want to enable and start the Productive Machine service now? (y/n): " START_SERVICE
if [[ "$START_SERVICE" == "y" ]]; then
    sudo systemctl daemon-reload
    sudo systemctl enable productive-machine
    sudo systemctl start productive-machine
    echo "Service started. You can check its status with: sudo systemctl status productive-machine"
else
    echo "Service installed but not started. You can start it manually with: sudo systemctl start productive-machine"
fi

# Set up Nginx if needed
read -p "Do you want to configure Nginx as a reverse proxy? (y/n): " CONFIGURE_NGINX
if [[ "$CONFIGURE_NGINX" == "y" ]]; then
    # Install Nginx if not installed
    if ! command -v nginx &> /dev/null; then
        echo "Installing Nginx..."
        sudo apt update
        sudo apt install -y nginx
    fi
    
    # Create self-signed certificate
    echo "Creating self-signed SSL certificate..."
    sudo mkdir -p /etc/ssl/private
    sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/ssl/private/nginx-selfsigned.key \
        -out /etc/ssl/certs/nginx-selfsigned.crt
    
    # Create Nginx config
    echo "Configuring Nginx..."
    cat > /tmp/productive-machine.conf << EOL
server {
    listen 80;
    server_name $(hostname);
    return 301 https://\$host\$request_uri;
}

server {
    listen 443 ssl;
    server_name $(hostname);

    ssl_certificate /etc/ssl/certs/nginx-selfsigned.crt;
    ssl_certificate_key /etc/ssl/private/nginx-selfsigned.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers on;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_session_cache shared:SSL:10m;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOL

    sudo mv /tmp/productive-machine.conf /etc/nginx/sites-available/productive-machine
    sudo ln -sf /etc/nginx/sites-available/productive-machine /etc/nginx/sites-enabled/
    sudo nginx -t && sudo systemctl restart nginx
    
    echo "Nginx configured. You can access the application at https://$(hostname)"
fi

# Configure firewall if ufw is installed
if command -v ufw &> /dev/null; then
    read -p "Do you want to configure the firewall? (y/n): " CONFIGURE_FIREWALL
    if [[ "$CONFIGURE_FIREWALL" == "y" ]]; then
        echo "Configuring firewall..."
        sudo ufw allow ssh
        sudo ufw allow 80/tcp
        sudo ufw allow 443/tcp
        
        # Only enable if not already enabled (to avoid locking yourself out)
        if [[ $(sudo ufw status | grep -c "Status: active") -eq 0 ]]; then
            sudo ufw --force enable
        fi
        
        echo "Firewall configured."
    fi
fi

# Print final instructions
echo "==================================================================="
echo "Productive Machine setup completed!"
echo "==================================================================="
echo ""
echo "Next steps:"
echo "1. Access the web interface at https://$(hostname) or http://localhost:5000"
echo "2. Log in with the default admin account:"
echo "   - Username: admin@example.com"
echo "   - Password: Admin123!"
echo "3. Change the default password immediately"
echo "4. Set up 2FA for enhanced security"
echo ""
echo "For more detailed instructions, see docs/SETUP.md"
echo "===================================================================" 