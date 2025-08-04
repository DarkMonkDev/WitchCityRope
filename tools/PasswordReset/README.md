# Password Reset Tool

A command-line tool for resetting user passwords in the WitchCityRope database.

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database running with the WitchCityRope schema
- Proper connection string configured in `appsettings.json`

## Configuration

Before running the tool, ensure the database connection string in `appsettings.json` is correct:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
  }
}
```

## Building the Tool

From the PasswordReset directory:

```bash
dotnet build
```

## Usage

```bash
dotnet run -- <email> <new-password>
```

### Examples

Reset password for admin user:
```bash
dotnet run -- admin@witchcityrope.com NewPassword123!
```

Reset password for a member:
```bash
dotnet run -- member@witchcityrope.com SecurePass456!
```

## Password Requirements

The new password must meet these requirements:
- At least 6 characters long
- Contains at least one digit (0-9)
- Contains at least one lowercase letter (a-z)
- Contains at least one uppercase letter (A-Z)
- Contains at least one non-alphanumeric character (!@#$%^&* etc.)

## Features

- Uses ASP.NET Core Identity's UserManager for secure password handling
- Validates password against configured password policy
- Updates the LastPasswordChangeAt timestamp
- Provides clear error messages if the operation fails
- Logs all operations for debugging

## Error Handling

The tool will exit with code 1 and display an error message if:
- The user email is not found in the database
- The new password doesn't meet requirements
- Database connection fails
- Any other unexpected error occurs

## Security Notes

- Passwords are hashed using ASP.NET Core Identity's secure hashing algorithm
- The tool doesn't display or log the actual password
- Always use strong passwords in production environments