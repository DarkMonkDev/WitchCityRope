using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VettedMemberImport.Data;
using VettedMemberImport.Services;

namespace VettedMemberImport;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Vetted Member Import Tool ===");
        Console.WriteLine();

        // Parse command line arguments
        var config = ParseArguments(args);
        if (config == null)
        {
            PrintUsage();
            return 1;
        }

        // Build configuration
        var environment = config.Environment ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add database context
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: Connection string 'Default' not found in configuration");
            return 1;
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Add services
        services.AddSingleton<DateParser>();
        services.AddScoped<CsvReader>();
        services.AddScoped<UserImporter>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Environment: {Environment}", environment);
            logger.LogInformation("Input File: {InputFile}", config.InputFile);
            logger.LogInformation("Dry Run: {IsDryRun}", config.IsDryRun);
            logger.LogInformation("Import Status: {ImportStatus}", config.ImportStatus);
            logger.LogInformation("  VettingStatus: {VettingStatus} ({StatusName})",
                config.VettingStatus, config.VettingStatus == 3 ? "Approved" : "InterviewApproved");
            logger.LogInformation("  WorkflowStatus: {WorkflowStatus} ({StatusName})",
                config.WorkflowStatus, config.WorkflowStatus == 3 ? "Approved" : "InterviewApproved");
            logger.LogInformation("Connection String: {ConnectionString}", MaskConnectionString(connectionString));

            // Test database connection
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!await context.Database.CanConnectAsync())
                {
                    logger.LogError("ERROR: Cannot connect to database");
                    return 1;
                }
                logger.LogInformation("Database connection successful");
            }

            // Read CSV file
            var csvReader = serviceProvider.GetRequiredService<CsvReader>();
            var rows = csvReader.ReadCsvFile(config.InputFile);
            logger.LogInformation("Read {Count} rows from CSV file", rows.Count);

            if (rows.Count == 0)
            {
                logger.LogWarning("No rows found in CSV file");
                return 0;
            }

            // Import users
            using (var scope = serviceProvider.CreateScope())
            {
                var importer = scope.ServiceProvider.GetRequiredService<UserImporter>();
                var summary = await importer.ImportUsersAsync(rows, config.IsDryRun, config.VettingStatus, config.WorkflowStatus);

                // Print summary
                Console.WriteLine();
                Console.WriteLine("=== IMPORT SUMMARY ===");
                Console.WriteLine($"Total Records: {summary.TotalRecords}");
                Console.WriteLine($"Successful: {summary.SuccessCount}");
                Console.WriteLine($"Skipped (Duplicates): {summary.SkippedCount}");
                Console.WriteLine($"Errors: {summary.ErrorCount}");
                Console.WriteLine();

                if (summary.Warnings.Count > 0)
                {
                    Console.WriteLine("=== WARNINGS ===");
                    foreach (var warning in summary.Warnings)
                    {
                        Console.WriteLine(warning);
                    }
                    Console.WriteLine();
                }

                if (summary.Errors.Count > 0)
                {
                    Console.WriteLine("=== ERRORS ===");
                    foreach (var error in summary.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    Console.WriteLine();
                }

                if (config.IsDryRun)
                {
                    Console.WriteLine("DRY RUN COMPLETE - No changes made to database");
                }
                else
                {
                    Console.WriteLine("IMPORT COMPLETE");
                }

                return summary.ErrorCount > 0 ? 1 : 0;
            }
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError("ERROR: File not found - {Message}", ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during import");
            return 1;
        }
    }

    static ImportConfig? ParseArguments(string[] args)
    {
        var config = new ImportConfig();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg == "--input" && i + 1 < args.Length)
            {
                config.InputFile = args[++i];
            }
            else if (arg == "--dry-run")
            {
                config.IsDryRun = true;
            }
            else if (arg == "--environment" && i + 1 < args.Length)
            {
                config.Environment = args[++i];
            }
            else if (arg == "--status" && i + 1 < args.Length)
            {
                config.ImportStatus = args[++i].ToLower();
            }
            else if (arg == "--help" || arg == "-h")
            {
                return null;
            }
        }

        // Validate required arguments
        if (string.IsNullOrWhiteSpace(config.InputFile))
        {
            Console.WriteLine("ERROR: --input parameter is required");
            return null;
        }

        if (!File.Exists(config.InputFile))
        {
            Console.WriteLine($"ERROR: Input file not found: {config.InputFile}");
            return null;
        }

        // Validate and convert status parameter
        if (config.ImportStatus == "approved")
        {
            config.VettingStatus = 3; // Approved
            config.WorkflowStatus = 3; // Approved
        }
        else if (config.ImportStatus == "interview-approved")
        {
            config.VettingStatus = 1; // InterviewApproved
            config.WorkflowStatus = 1; // InterviewApproved
        }
        else
        {
            Console.WriteLine($"ERROR: Invalid --status value: {config.ImportStatus}");
            Console.WriteLine("       Allowed values: 'approved' or 'interview-approved'");
            return null;
        }

        return config;
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: VettedMemberImport --input <csv-file> [options]");
        Console.WriteLine();
        Console.WriteLine("Required:");
        Console.WriteLine("  --input <file>          Path to CSV file exported from Google Sheet");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --dry-run               Test import without writing to database");
        Console.WriteLine("  --environment <env>     Environment (Development, Staging, Production)");
        Console.WriteLine("                          Default: Development");
        Console.WriteLine("  --status <status>       Import status: 'approved' or 'interview-approved'");
        Console.WriteLine("                          Default: approved");
        Console.WriteLine("                          - approved: Fully vetted members (VettingStatus=3)");
        Console.WriteLine("                          - interview-approved: Approved for interview (VettingStatus=1)");
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Dry run with local database");
        Console.WriteLine("  dotnet run -- --input=vetted-members.csv --dry-run");
        Console.WriteLine();
        Console.WriteLine("  # Actual import to local database (fully vetted)");
        Console.WriteLine("  dotnet run -- --input=vetted-members.csv");
        Console.WriteLine();
        Console.WriteLine("  # Import interview-approved members");
        Console.WriteLine("  dotnet run -- --input=pre-vetted.csv --status=interview-approved");
        Console.WriteLine();
        Console.WriteLine("  # Import to staging");
        Console.WriteLine("  dotnet run -- --input=vetted-members.csv --environment=Staging");
        Console.WriteLine();
        Console.WriteLine("  # Import interview-approved to production");
        Console.WriteLine("  dotnet run -- --input=pre-vetted.csv --status=interview-approved --environment=Production");
        Console.WriteLine();
    }

    static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for logging
        var parts = connectionString.Split(';');
        var masked = new List<string>();

        foreach (var part in parts)
        {
            if (part.TrimStart().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                masked.Add("Password=***");
            }
            else
            {
                masked.Add(part);
            }
        }

        return string.Join(";", masked);
    }
}

class ImportConfig
{
    public string InputFile { get; set; } = string.Empty;
    public bool IsDryRun { get; set; } = false;
    public string? Environment { get; set; } = "Development";
    public string ImportStatus { get; set; } = "approved";
    public int VettingStatus { get; set; } = 3; // Approved
    public int WorkflowStatus { get; set; } = 3; // Approved
}
