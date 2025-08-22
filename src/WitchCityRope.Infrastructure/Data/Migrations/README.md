# Entity Framework Core Migrations

This folder contains the database migrations for the WitchCityRope application.

## Prerequisites

Ensure you have the EF Core tools installed:
```bash
dotnet tool install --global dotnet-ef
```

## Creating Migrations

To create a new migration, run the following command from the solution root directory:

```bash
# From the solution root (where the .sln file would be)
dotnet ef migrations add InitialCreate --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api

# Or from the Infrastructure project directory
cd src/WitchCityRope.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WitchCityRope.Api
```

## Applying Migrations

To update the database with the latest migrations:

```bash
# From the solution root
dotnet ef database update --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api

# Or from the Api project directory
cd src/WitchCityRope.Api
dotnet ef database update
```

## Other Useful Commands

### List migrations
```bash
dotnet ef migrations list --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api
```

### Remove last migration (if not applied to database)
```bash
dotnet ef migrations remove --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api
```

### Generate SQL script
```bash
dotnet ef migrations script --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api --output migrations.sql
```

### Drop database (use with caution!)
```bash
dotnet ef database drop --project src/WitchCityRope.Infrastructure --startup-project src/WitchCityRope.Api
```

## Notes

- The database file (`witchcityrope.db`) will be created in the Api project directory
- Make sure to run migrations after pulling changes that include new migration files
- Always review migration files before applying them to production databases