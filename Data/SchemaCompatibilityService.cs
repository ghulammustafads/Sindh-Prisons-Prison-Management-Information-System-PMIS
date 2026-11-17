using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PrisonManagementSystem.Data;

public class DatabaseStatusResult
{
    public bool IsConnected { get; set; }
    public string ConfiguredDatabaseName { get; set; } = "PrisonManagementDB";
    public string? ActualDatabaseName { get; set; }
    public bool IsDatabaseNameCorrect => string.Equals(ConfiguredDatabaseName, ActualDatabaseName, StringComparison.OrdinalIgnoreCase);
    public List<string> FoundTables { get; set; } = new();
    public List<string> MissingTables { get; set; } = new();
    public bool IsSchemaValid => IsConnected && IsDatabaseNameCorrect && MissingTables.Count == 0;
    public string? ErrorMessage { get; set; }
    public string? TechnicalDetails { get; set; }
    public string? RemediationAdvice { get; set; }
}

public interface ISchemaCompatibilityService
{
    Task<DatabaseStatusResult> CheckCompatibilityAsync(CancellationToken cancellationToken = default);
}

public class SchemaCompatibilityService : ISchemaCompatibilityService
{
    private readonly PrisonDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchemaCompatibilityService> _logger;

    private static readonly string[] ExpectedTables =
    [
        "StaffRoles",
        "Barracks",
        "Staff",
        "ManagementAccounts",
        "Cells",
        "Prisoners",
        "CourtCases",
        "CourtTrials",
        "JailTimetable",
        "GateRegister"
    ];

    public SchemaCompatibilityService(
        PrisonDbContext context,
        IConfiguration configuration,
        ILogger<SchemaCompatibilityService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DatabaseStatusResult> CheckCompatibilityAsync(CancellationToken cancellationToken = default)
    {
        var result = new DatabaseStatusResult();
        var connStr = _configuration.GetConnectionString("PrisonDatabase") ?? string.Empty;

        try
        {
            var builder = new SqlConnectionStringBuilder(connStr);
            result.ConfiguredDatabaseName = builder.InitialCatalog;
        }
        catch
        {
            result.ConfiguredDatabaseName = "PrisonManagementDB";
        }

        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                result.IsConnected = false;
                result.ErrorMessage = "Database connection unavailable. SQL Server is not reachable at localhost\\SQLEXPRESS.";
                result.RemediationAdvice = "Verify that SQL Server Express service is running locally on 'localhost\\SQLEXPRESS' and accepts Windows Authentication.";
                return result;
            }

            result.IsConnected = true;

            // Check actual connected database name
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT DB_NAME();";
            await _context.Database.OpenConnectionAsync(cancellationToken);

            var dbNameObj = await command.ExecuteScalarAsync(cancellationToken);
            result.ActualDatabaseName = dbNameObj?.ToString() ?? string.Empty;

            if (!result.IsDatabaseNameCorrect)
            {
                if (string.Equals(result.ActualDatabaseName, "PrisonManagementInitialDB", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = "Database name mismatch: Connected to legacy 'PrisonManagementInitialDB' instead of required 'PrisonManagementDB'.";
                    result.RemediationAdvice = "Update appsettings.Database.json or your SQL Server instance to attach the verified 'PrisonManagementDB' database.";
                }
                else
                {
                    result.ErrorMessage = $"Database name mismatch: Connected to '{result.ActualDatabaseName}' instead of '{result.ConfiguredDatabaseName}'.";
                    result.RemediationAdvice = $"Ensure the database is named '{result.ConfiguredDatabaseName}'.";
                }
            }

            // Inspect tables
            command.CommandText = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE';";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }

            foreach (var expected in ExpectedTables)
            {
                if (tables.Contains(expected))
                {
                    result.FoundTables.Add(expected);
                }
                else
                {
                    result.MissingTables.Add(expected);
                }
            }

            if (result.MissingTables.Count > 0)
            {
                result.ErrorMessage = $"Missing required database tables: {string.Join(", ", result.MissingTables)}.";
                result.RemediationAdvice = "Execute the official Sindh Prisons PMIS database creation script on SQL Server to generate the 10 standard tables.";
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server connection or execution error.");
            result.IsConnected = false;
            result.ErrorMessage = "Unable to connect to SQL Server database.";
            result.TechnicalDetails = ex.Message;
            result.RemediationAdvice = "Check that SQL Server (localhost\\SQLEXPRESS) is online and the connection string in appsettings.Database.json is accessible.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying database status.");
            result.IsConnected = false;
            result.ErrorMessage = "Database verification error.";
            result.TechnicalDetails = ex.Message;
        }

        return result;
    }
}
