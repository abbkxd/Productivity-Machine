# Productive Machine Testing Guide

This document provides instructions for manually testing the Productive Machine application's functionality. It covers all major features and components of the system.

## Prerequisites

1. The application should be running on your Raspberry Pi or local development environment.
2. You should have admin access to the application (use the default admin account: admin@example.com / Admin123!).
3. You should have a mobile device with an authenticator app (e.g., Google Authenticator, Authy) for 2FA testing.

## Running Automated Tests

Before performing manual tests, run the automated test suite to verify core functionality:

```bash
# Navigate to the test project directory
cd src/ProductiveMachine.Tests

# Run the tests
dotnet test
```

## Manual Testing Checklist

### 1. User Authentication

#### 1.1 User Registration
- [ ] Navigate to the registration page
- [ ] Create a new user account with valid credentials
- [ ] Verify you receive a confirmation message
- [ ] Try registering with the same email and verify it's rejected
- [ ] Try registering with a weak password and verify it's rejected

#### 1.2 User Login
- [ ] Log in with the newly created user account
- [ ] Verify you can access user-specific features
- [ ] Log in with invalid credentials and verify it's rejected
- [ ] Test the "Forgot Password" functionality

#### 1.3 Two-Factor Authentication
- [ ] Navigate to user profile/security settings
- [ ] Enable 2FA for your account
- [ ] Scan the QR code with an authenticator app
- [ ] Verify the code from the app works for login
- [ ] Log out and log back in to verify 2FA is required
- [ ] Disable 2FA and verify login works without it

### 2. Todo Management

#### 2.1 Create To-Do Items
- [ ] Create a simple to-do item with title only
- [ ] Create a detailed to-do item with description, due date, category, and priority
- [ ] Verify the item appears in your to-do list

#### 2.2 Manage To-Do Items
- [ ] Edit an existing to-do item and save changes
- [ ] Mark a to-do item as "In Progress"
- [ ] Mark a to-do item as "Completed"
- [ ] Verify completed items are properly displayed or filtered
- [ ] Delete a to-do item and verify it's removed

#### 2.3 Categories
- [ ] Create a new category
- [ ] Assign a color to the category
- [ ] Create a to-do item with the new category
- [ ] Edit a category name/color
- [ ] Delete a category and verify that items using it are updated

#### 2.4 Filtering and Sorting
- [ ] Filter to-do items by category
- [ ] Filter to-do items by status
- [ ] Filter to-do items by due date
- [ ] Sort to-do items by priority
- [ ] Sort to-do items by due date

#### 2.5 Recurring Tasks
- [ ] Create a daily recurring task
- [ ] Create a weekly recurring task (on specific days)
- [ ] Create a monthly recurring task
- [ ] Complete a recurring task and verify the next occurrence is generated
- [ ] Edit a recurring schedule and verify changes are applied

### 3. Journal Management

#### 3.1 Create Journal Entries
- [ ] Create a new journal entry with title and content
- [ ] Add tags to the journal entry
- [ ] Add a mood to the journal entry
- [ ] Verify the entry appears in your journal list

#### 3.2 Manage Journal Entries
- [ ] Edit an existing journal entry
- [ ] Verify the modification date is updated
- [ ] Delete a journal entry and verify it's removed

#### 3.3 Journal Search and Filtering
- [ ] Search for journal entries by content
- [ ] Search for journal entries by title
- [ ] Filter journal entries by date range
- [ ] Filter journal entries by mood
- [ ] Filter journal entries by tag

### 4. Self-Destructing Emails

#### 4.1 Create Self-Destructing Emails
- [ ] Create a new self-destructing email
- [ ] Set an expiration period (default is 2 days)
- [ ] Send the email to a valid email address (can be your own)

#### 4.2 Access Self-Destructing Emails
- [ ] Check your email inbox for the link
- [ ] Access the email content using the link
- [ ] Verify the content is displayed correctly
- [ ] Try accessing the link again and verify it no longer works (already accessed)

#### 4.3 Email Expiration
- [ ] Create a self-destructing email with a short expiration (e.g., 1 hour)
- [ ] Wait for it to expire
- [ ] Verify the email is marked as expired and cannot be accessed
- [ ] Verify the cleanup job removes expired emails (this may require checking the database or logs)

### 5. Backup and Recovery

#### 5.1 Manual Backup
- [ ] Navigate to the backup section of the admin panel
- [ ] Initiate a manual backup
- [ ] Verify the backup is created successfully
- [ ] Check the backup log for details

#### 5.2 Automated Backup
- [ ] Configure automated backup settings
- [ ] Wait for the scheduled backup or trigger it manually
- [ ] Verify the backup job runs successfully
- [ ] Check the backup log for details

#### 5.3 GPG Encryption
- [ ] Configure GPG encryption for backups
- [ ] Create a backup with encryption enabled
- [ ] Verify the backup file is encrypted
- [ ] Test decrypting the backup file with your GPG key

#### 5.4 Cloud Backup
- [ ] Configure rclone for cloud backup
- [ ] Create a backup with cloud upload enabled
- [ ] Verify the backup is uploaded to the cloud storage
- [ ] Check the remote path for the backup file

### 6. Application Performance

#### 6.1 Load Testing
- [ ] Create multiple to-do items and journal entries (20+ of each)
- [ ] Verify the application remains responsive
- [ ] Check the database size after adding multiple entries

#### 6.2 Concurrent Usage
- [ ] Access the application from multiple devices/browsers simultaneously
- [ ] Perform operations from different sessions
- [ ] Verify that changes are properly synchronized and saved

### 7. Security Testing

#### 7.1 Access Control
- [ ] Try accessing user-specific data when not logged in
- [ ] Try accessing another user's data when logged in (should be denied)
- [ ] Try accessing admin features as a regular user (should be denied)

#### 7.2 Input Validation
- [ ] Try submitting forms with invalid data
- [ ] Try inserting potentially malicious inputs (SQL injection, XSS)
- [ ] Verify that input validation properly handles these cases

## Reporting Issues

If you encounter any issues during testing, please document them with the following information:

1. Feature/functionality being tested
2. Steps to reproduce the issue
3. Expected behavior
4. Actual behavior
5. Screenshots (if applicable)
6. Environment details (browser, device, OS)

Submit issues to the project repository or contact the development team directly.

## Test Data Cleanup

After completing the tests, you may want to clean up test data:

- Delete test to-do items, categories, and journal entries
- Delete test users
- Delete test self-destructing emails
- Remove test backup files 