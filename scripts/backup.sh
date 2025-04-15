#!/bin/bash

# Productive Machine Backup Script
# This script creates a backup of the SQLite database and uploads it to cloud storage

# Backup directory
BACKUP_DIR="../backups"
DATA_DIR="../data"
DB_FILE="$DATA_DIR/productive_machine.db"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/productive_machine_backup_$TIMESTAMP.db"
GPG_RECIPIENT="your-email@example.com" # Change this to your GPG key email

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

echo "Starting database backup..."

# Check if database file exists
if [ ! -f "$DB_FILE" ]; then
    echo "Error: Database file not found at $DB_FILE"
    exit 1
fi

# Create a copy of the database
echo "Creating database copy..."
sqlite3 "$DB_FILE" ".backup '$BACKUP_FILE'"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Backup creation failed"
    exit 1
fi

echo "Backup created at $BACKUP_FILE"

# Encrypt the backup if GPG is available
if command -v gpg &> /dev/null && [ -n "$GPG_RECIPIENT" ]; then
    echo "Encrypting backup..."
    gpg --output "$BACKUP_FILE.gpg" --encrypt --recipient "$GPG_RECIPIENT" "$BACKUP_FILE"
    
    if [ -f "$BACKUP_FILE.gpg" ]; then
        echo "Backup encrypted as $BACKUP_FILE.gpg"
        rm "$BACKUP_FILE"
        BACKUP_FILE="$BACKUP_FILE.gpg"
    else
        echo "Warning: Encryption failed, keeping unencrypted backup"
    fi
fi

# Upload to cloud storage if rclone is configured
if command -v rclone &> /dev/null; then
    echo "Uploading backup to cloud storage..."
    RCLONE_CONFIG="/etc/rclone/rclone.conf"
    REMOTE_NAME="remote" # Change this to your configured remote name in rclone
    REMOTE_DIR="productive_machine_backups"
    
    if [ -f "$RCLONE_CONFIG" ]; then
        rclone copy "$BACKUP_FILE" "$REMOTE_NAME:$REMOTE_DIR" --config "$RCLONE_CONFIG"
        if [ $? -eq 0 ]; then
            echo "Backup uploaded to cloud storage"
        else
            echo "Warning: Failed to upload backup to cloud storage"
        fi
    else
        echo "Warning: rclone config not found at $RCLONE_CONFIG"
    fi
else
    echo "Warning: rclone not installed, skipping cloud upload"
fi

echo "Backup process completed" 