using System.Diagnostics;
using GroceryHub.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;
using Xunit;

namespace GroceryHub.IntegrationTests;

[CollectionDefinition(CollectionName)]
public sealed class SqlServerIntegrationCollection : ICollectionFixture<SqlServerTestDatabaseFixture>
{
    public const string CollectionName = "SqlServerIntegration";
}

public sealed class SqlServerTestDatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _msSqlContainer;
    private Respawner? _respawner;
    private string? _localDbMasterConnectionString;

    public string DatabaseName { get; private set; } = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        DatabaseName = $"GroceryHub_IntTests_{Guid.NewGuid():N}";

        if (await TryStartDockerSqlServerAsync())
        {
            SqlConnectionStringBuilder builder = new(_msSqlContainer!.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
                MultipleActiveResultSets = true,
                TrustServerCertificate = true,
            };
            ConnectionString = builder.ConnectionString;
        }
        else
        {
            await EnsureLocalDbRunningAsync();
            _localDbMasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            await using (SqlConnection masterConnection = new(_localDbMasterConnectionString))
            {
                await masterConnection.OpenAsync();
                await using SqlCommand createDbCommand = masterConnection.CreateCommand();
                createDbCommand.CommandText = $"CREATE DATABASE [{DatabaseName}];";
                await createDbCommand.ExecuteNonQueryAsync();
            }

            ConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        }

        await using GroceryHubDbContext dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude =
                [
                    "identity",
                    "customers",
                    "catalog",
                    "inventory",
                    "purchasing",
                    "promotions",
                    "cart",
                    "ordering",
                    "payments",
                    "reviews",
                    "audit",
                    "shared",
                ],
                TablesToIgnore =
                [
                    new Table("shared", "__EFMigrationsHistory"),
                ],
            });
    }

    public GroceryHubDbContext CreateDbContext()
    {
        DbContextOptions<GroceryHubDbContext> options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseSqlServer(
                ConnectionString,
                sql => sql.MigrationsHistoryTable(
                    PersistenceServiceCollectionExtensions.MigrationsHistoryTableName,
                    DatabaseSchemas.Shared))
            .Options;

        return new GroceryHubDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner has not been initialized.");
        }

        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        if (_msSqlContainer is not null)
        {
            await _msSqlContainer.DisposeAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_localDbMasterConnectionString) && !string.IsNullOrWhiteSpace(DatabaseName))
        {
            await using SqlConnection masterConnection = new(_localDbMasterConnectionString);
            await masterConnection.OpenAsync();
            await using SqlCommand dropCommand = masterConnection.CreateCommand();
            dropCommand.CommandText = $"""
                IF DB_ID(N'{DatabaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DatabaseName}];
                END
                """;
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task<bool> TryStartDockerSqlServerAsync()
    {
        if (!IsDockerDaemonLikelyPresent())
        {
            return false;
        }

        try
        {
            _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU16-ubuntu-22.04")
                .Build();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(45));
            await _msSqlContainer.StartAsync(cts.Token);
            return true;
        }
        catch
        {
            if (_msSqlContainer is not null)
            {
                try
                {
                    await _msSqlContainer.DisposeAsync();
                }
                catch
                {
                    // Ignore cleanup errors when Docker is unreachable.
                }

                _msSqlContainer = null;
            }

            return false;
        }
    }

    private static bool IsDockerDaemonLikelyPresent()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            return Directory.GetFiles(@"\\.\pipe\", "*docker*").Length > 0;
        }

        return File.Exists("/var/run/docker.sock");
    }

    private static async Task EnsureLocalDbRunningAsync()
    {
        ProcessStartInfo psi = new("sqllocaldb", "start MSSQLLocalDB")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process? process = Process.Start(psi);
        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }
}
