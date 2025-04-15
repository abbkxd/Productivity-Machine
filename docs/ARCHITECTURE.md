# Productive Machine - Architecture Documentation

This document explains the architecture of the Productive Machine application, detailing the system components, data flow, and technical design decisions.

## System Overview

Productive Machine is a self-hosted productivity application designed to run on a Raspberry Pi, providing todo management, journaling, self-destructing emails, and automated backups. The application follows a modern, layered architecture with a clear separation of concerns.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Devices                          │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐ │
│  │   Web     │  │   Mobile  │  │   Tablet  │  │   Other   │ │
│  │  Browser  │  │  Browser  │  │  Browser  │  │  Devices  │ │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘ │
└────────┼───────────────┼───────────────┼───────────────┼────┘
         │               │               │               │     
         └───────────────┼───────────────┼───────────────┘     
                         │               │                     
                         ▼               ▼                     
┌─────────────────────────────────────────────────────────────┐
│                      Raspberry Pi                           │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                      Nginx                          │    │
│  │  (Reverse Proxy, SSL Termination, Static Content)   │    │
│  └──────────────────────────┬──────────────────────────┘    │
│                             │                               │
│  ┌──────────────────────────▼──────────────────────────┐    │
│  │             ASP.NET Core Web Application            │    │
│  │                                                     │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │    │
│  │  │  Controllers│  │   Services  │  │Repositories │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │    │
│  │                                                     │    │
│  └──────────────────────────┬──────────────────────────┘    │
│                             │                               │
│  ┌──────────────────────────▼──────────────────────────┐    │
│  │                  Hangfire Jobs                      │    │
│  │                                                     │    │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐     │    │
│  │  │ Email      │  │ Database   │  │ Task       │     │    │
│  │  │ Cleanup    │  │ Backup     │  │ Maintenance│     │    │
│  │  └────────────┘  └────────────┘  └────────────┘     │    │
│  └──────────────────────────┬──────────────────────────┘    │
│                             │                               │
│  ┌──────────────────────────▼──────────────────────────┐    │
│  │                  SQLite Database                     │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Web Application (ASP.NET Core)

The web application is built using ASP.NET Core and follows the Model-View-Controller (MVC) pattern:

- **Presentation Layer**
  - **Controllers**: Handle HTTP requests and responses
  - **Views**: Razor pages that render the user interface
  - **Models**: Data transfer objects (DTOs) for UI presentation

- **Business Logic Layer**
  - **Services**: Implement application logic and business rules
  - **Domain Models**: Represent core business entities
  - **Validators**: Ensure data integrity and validity

- **Data Access Layer**
  - **Repositories**: Abstract the data storage and retrieval operations
  - **Entity Framework Core**: ORM for database interactions
  - **Database Context**: Defines the session with the database

### 2. Background Processing (Hangfire)

The application uses Hangfire for scheduling and executing background jobs:

- **Email Cleanup Job**: Automatically removes expired self-destructing emails
- **Database Backup Job**: Creates regular backups of the SQLite database
- **Task Maintenance Job**: Processes recurring tasks and handles overdue notifications

### 3. Database (SQLite)

SQLite was chosen as the database engine for its simplicity, reliability, and zero-configuration nature, making it perfect for a Raspberry Pi deployment:

- **Tables**: Users, Todos, TodoCategories, JournalEntries, SelfDestructingEmails, BackupLogs
- **Relationships**: Properly defined with foreign key constraints
- **Indexes**: Optimized for common query patterns

### 4. Authentication & Security

- **JWT Authentication**: Secure token-based authentication
- **Two-Factor Authentication**: Enhanced security via TOTP (Time-based One-Time Password)
- **Password Hashing**: Uses modern hashing algorithms with salt
- **Authorization Policies**: Role-based access control

### 5. External Services Integration

- **Email Service**: SMTP integration for sending self-destructing emails
- **Cloud Backup**: rclone integration for secure offsite backups
- **GPG Encryption**: Secure encryption of sensitive backup data

## Data Flow

### Todo Management Flow

1. User creates/updates a todo item through the web interface
2. The TodoController receives the request and validates input
3. The TodoService processes the business logic
4. The TodoRepository persists changes to the database
5. For recurring todos, the TaskMaintenanceJob handles scheduling

### Self-Destructing Email Flow

1. User creates a self-destructing email via the web interface
2. The EmailController validates the input and sends to EmailService
3. EmailService sends the email via SMTP and stores metadata
4. EmailCleanupJob periodically checks for expired emails
5. Expired emails are permanently deleted from the database

### Backup Flow

1. DatabaseBackupJob is triggered on schedule (e.g., daily)
2. The job creates a SQLite database dump
3. If configured, the backup is encrypted using GPG
4. The backup is stored in the local backup directory
5. If cloud backup is enabled, the backup is uploaded via rclone
6. Backup results are logged in the BackupLog table

## Technical Design Decisions

### Why SQLite?

SQLite was chosen over other database systems for several reasons:
- **Simplicity**: Zero configuration, server-less operation
- **Performance**: Excellent on low-powered devices like Raspberry Pi
- **Reliability**: ACID compliant with crash recovery
- **Resource Efficiency**: Low memory and CPU footprint
- **Easy Backup**: Single file that can be easily backed up

### Why ASP.NET Core?

ASP.NET Core provides:
- **Cross-platform**: Runs on Linux, including Raspberry Pi OS
- **Performance**: High-performance web framework
- **Security**: Built-in features for authentication and authorization
- **Dependency Injection**: Clean architecture with testable components
- **Entity Framework Core**: Powerful ORM for database operations

### Why Hangfire?

Hangfire was selected for background processing because:
- **Persistence**: Jobs survive application restarts
- **Scheduling**: Supports recurring jobs with cron expressions
- **Dashboard**: Visual monitoring of job execution
- **Retries**: Automatic retry mechanism for failed jobs
- **Queue Priority**: Supports job priorities and queue management

## Extensibility Points

The application is designed to be extensible in various ways:

1. **New Todo Types**: The system allows for adding new types of todos through inheritance
2. **Authentication Providers**: The authentication system can be extended with new providers
3. **Cloud Storage Providers**: Additional cloud backup destinations can be integrated
4. **Notification Channels**: The notification system can be extended beyond email

## Performance Considerations

The application is optimized for Raspberry Pi environments:

- **Database Indexing**: Key fields are indexed for faster querying
- **Lazy Loading**: Resources are loaded only when needed
- **Caching**: Frequently accessed data is cached to reduce database load
- **Pagination**: Large result sets are paginated to conserve memory
- **Asset Optimization**: CSS and JavaScript are minified and bundled

## Security Measures

The application implements several security measures:

- **HTTPS**: All traffic is encrypted with SSL/TLS
- **JWT with Short Lifespan**: Authentication tokens expire quickly
- **Two-Factor Authentication**: Additional security layer
- **Input Validation**: All user inputs are validated
- **CSRF Protection**: Cross-Site Request Forgery protection
- **XSS Prevention**: Cross-Site Scripting prevention
- **Encrypted Backups**: Sensitive data is encrypted at rest
- **Least Privilege Principle**: Components only have access to what they need

## Deployment Model

The application is designed for a single-instance deployment on a Raspberry Pi:

1. **Web Application**: Runs as a systemd service for automatic restart
2. **Nginx**: Acts as a reverse proxy, handling SSL termination
3. **Firewall**: UFW configured to only allow necessary traffic
4. **Database**: Stored in a dedicated data directory with proper permissions
5. **Backups**: Automated local and optional cloud backups

## Monitoring and Logging

The application includes:

- **Application Logs**: Structured logging with Serilog
- **Job Dashboard**: Hangfire dashboard for monitoring background jobs
- **Health Checks**: Endpoints to verify system health
- **Backup Logs**: Detailed logs of backup operations
- **Email Logs**: Tracking of email deliveries

## Conclusion

The Productive Machine architecture is designed to be robust, secure, and efficient while running on modest hardware like a Raspberry Pi. The clear separation of concerns, modular design, and appropriate technology choices ensure the application is both maintainable and extensible. 