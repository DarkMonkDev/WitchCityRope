namespace WitchCityRope.Api.Features.Backup.Models;

public class BackupConfiguration
{
    public DatabaseConfig Database { get; set; } = new();
    public SpacesConfig Spaces { get; set; } = new();
    public BackupOptions BackupOptions { get; set; } = new();
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string DatabaseName { get; set; } = "witchcityrope_dev";
    public string SchemaName { get; set; } = "public";
    public string Username { get; set; } = "witchcity_user";
    public string Password { get; set; } = string.Empty;
}

public class SpacesConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string FolderPrefix { get; set; } = "backups";
}

public class BackupOptions
{
    public string CompressionFormat { get; set; } = "custom";
    public string TempDirectory { get; set; } = "/tmp";
    public int TimeoutSeconds { get; set; } = 300;
}
