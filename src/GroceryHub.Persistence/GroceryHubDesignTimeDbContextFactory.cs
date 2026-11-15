using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GroceryHub.Persistence;

public sealed class GroceryHubDesignTimeDbContextFactory : IDesignTimeDbContextFactory<GroceryHubDbContext>
{
    public GroceryHubDbContext CreateDbContext(string[] args)
    {
        string connectionString = ResolveConnectionString(args);

        DbContextOptionsBuilder<GroceryHubDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable(
                PersistenceServiceCollectionExtensions.MigrationsHistoryTableName,
                DatabaseSchemas.Shared));

        return new GroceryHubDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString(string[] args)
    {
        if (args is not null)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(args[i + 1]))
                {
                    return args[i + 1];
                }
            }
        }

        string? envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            return envConnection;
        }

        return "Server=(localdb)\\MSSQLLocalDB;Database=GroceryHub;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
