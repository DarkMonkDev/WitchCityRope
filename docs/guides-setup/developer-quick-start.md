# WitchCityRope Quick Start Guide

Welcome to WitchCityRope! This guide will help you get the application up and running on your local development environment in just a few minutes.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Running the Application](#running-the-application)
5. [First-Time Setup](#first-time-setup)
6. [Basic Usage](#basic-usage)
7. [Common Tasks](#common-tasks)
8. [Troubleshooting](#troubleshooting)

## Prerequisites

Before you begin, ensure you have the following installed on your system:

### Required Software

- **.NET 9.0 SDK or later**
  - Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
  - Verify installation: `dotnet --version`

- **Git**
  - Download from [https://git-scm.com/](https://git-scm.com/)
  - Verify installation: `git --version`

- **Code Editor** (choose one)
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended for Windows)
  - [Visual Studio Code](https://code.visualstudio.com/) with C# extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Optional Software

- **Docker Desktop** (for containerized development)
  - Download from [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)

- **Database Browser for SQLite**
  - [DB Browser for SQLite](https://sqlitebrowser.org/)
  - [SQLiteStudio](https://sqlitestudio.pl/)

## Installation

### Step 1: Clone the Repository

```bash
# Clone the repository
git clone https://github.com/yourusername/WitchCityRope.git

# Navigate to the project directory
cd WitchCityRope
```

### Step 2: Restore Dependencies

```bash
# Navigate to the web project
cd src/WitchCityRope.Web

# Restore NuGet packages
dotnet restore
```

## Configuration

### Step 1: Create Development Configuration

```bash
# Copy the template configuration
cp appsettings.json appsettings.Development.json
```

### Step 2: Configure API Keys

Edit `appsettings.Development.json` and add your API keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=witchcityrope.db"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "WitchCityRope",
    "Audience": "WitchCityRopeUsers",
    "ExpirationInMinutes": 1440
  },
  "SendGrid": {
    "ApiKey": "SG.your-sendgrid-api-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Witch City Rope"
  },
  "Syncfusion": {
    "LicenseKey": "your-syncfusion-license-key"
  },
  "PayPal": {
    "ClientId": "your-paypal-sandbox-client-id",
    "ClientSecret": "your-paypal-sandbox-secret",
    "Mode": "sandbox"
  },
  "Google": {
    "ClientId": "your-google-oauth-client-id",
    "ClientSecret": "your-google-oauth-secret"
  }
}
```

### Step 3: Obtain API Keys

#### Syncfusion License (Required)
1. Visit [Syncfusion Community License](https://www.syncfusion.com/products/communitylicense)
2. Sign up for a free community license (if eligible)
3. Copy your license key from the dashboard

#### SendGrid (Optional for Development)
1. Sign up at [SendGrid](https://sendgrid.com/)
2. Create an API key with "Mail Send" permissions
3. Verify a sender email address

#### PayPal Sandbox (Optional for Development)
1. Visit [PayPal Developer](https://developer.paypal.com/)
2. Create a sandbox app
3. Copy the Client ID and Secret

#### Google OAuth (Optional for Development)
1. Visit [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add `https://localhost:5652/signin-google` to authorized redirect URIs

## Running the Application

### Step 1: Create the Database

```bash
# Ensure you're in the Web project directory
cd src/WitchCityRope.Web

# Create the database and apply migrations
dotnet ef database update
```

### Step 2: Run the Application

```bash
# Run the application
dotnet run

# Or run with hot reload enabled
dotnet watch run
```

### Step 3: Access the Application

Open your browser and navigate to:
- **HTTPS**: [https://localhost:5652](https://localhost:5652)
- **HTTP**: [http://localhost:5651](http://localhost:5651)

## First-Time Setup

### Create an Admin User

1. Navigate to the registration page
2. Create a new account with your email
3. Open the SQLite database using a database browser
4. Find your user in the `Users` table
5. Update the `Role` column to `Admin`
6. Save the changes

### Using SQL Command

```sql
-- Update your user to admin (replace with your email)
UPDATE Users 
SET Role = 'Admin' 
WHERE Email = 'your-email@example.com';
```

### Seed Sample Data (Optional)

```bash
# Run the seed command
dotnet run -- --seed

# This will create:
# - Sample events
# - Test users
# - Demo vetting applications
```

## Basic Usage

### As a Guest User

1. **Browse Events**: View public classes on the homepage
2. **Register**: Create an account to access member features
3. **Apply for Membership**: Submit a vetting application

### As a Member

1. **View All Events**: Access both classes and meetups
2. **Register for Events**: RSVP or purchase tickets
3. **Manage Profile**: Update your scene name and preferences
4. **View Tickets**: Track your event registrations

### As an Admin

1. **Create Events**: Use the event creation wizard
2. **Review Applications**: Process vetting applications
3. **Manage Check-ins**: Use the mobile-friendly check-in interface
4. **View Reports**: Access financial and attendance analytics

## Common Tasks

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test category
dotnet test --filter Category=Unit
```

### Database Management

```bash
# Create a new migration
dotnet ef migrations add YourMigrationName

# Update database
dotnet ef database update

# Revert to previous migration
dotnet ef database update PreviousMigrationName

# Generate SQL script
dotnet ef migrations script
```

### Docker Development

```bash
# Build the Docker image
docker build -t witchcityrope .

# Run the container
docker run -p 5000:80 -e ASPNETCORE_ENVIRONMENT=Development witchcityrope

# Use Docker Compose
docker-compose up --build
```

## Troubleshooting

### Common Issues

#### "Syncfusion license key is invalid"
- Ensure you have a valid Syncfusion license key in your configuration
- Check that the key is correctly formatted (no extra spaces)
- Verify your license is active at [Syncfusion Dashboard](https://www.syncfusion.com/account/manage-trials/downloads)

#### "Database file not found"
```bash
# Ensure you've created the database
dotnet ef database update
```

#### "SSL certificate error in development"
```bash
# Trust the development certificate
dotnet dev-certs https --trust
```

#### "Port already in use"
```bash
# Change the port in Properties/launchSettings.json
# Or kill the process using the port
# Windows: netstat -ano | findstr :5001
# Linux/Mac: lsof -ti:5001 | xargs kill
```

### Getting Help

1. **Check the Documentation**: Review the `/docs` folder
2. **Search Issues**: Look for similar issues on GitHub
3. **Community Support**: Post in GitHub Discussions
4. **Debug Logs**: Check the console output and log files

### Useful Commands

```bash
# Clean and rebuild
dotnet clean
dotnet build

# List installed tools
dotnet tool list -g

# Update EF Core tools
dotnet tool update --global dotnet-ef

# Check for outdated packages
dotnet list package --outdated
```

## Next Steps

Now that you have WitchCityRope running locally:

1. **Explore the Codebase**: Familiarize yourself with the project structure
2. **Read the Architecture Docs**: Understand the vertical slice pattern
3. **Review the Style Guide**: Follow UI/UX conventions
4. **Run the Tests**: Ensure everything works correctly
5. **Create a Feature Branch**: Start contributing!

## Additional Resources

- [Architecture Documentation](../architecture/)
- [API Documentation](../api/)
- [Design System Guide](../design/FINAL-STYLE-GUIDE.md)
- [Contributing Guidelines](../../CONTRIBUTING.md)
- [Development Roadmap](../../ROADMAP.md)

---

Happy coding! If you encounter any issues not covered in this guide, please create an issue on GitHub or reach out to the development team.