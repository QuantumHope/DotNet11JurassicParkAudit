using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParkOps.Data;

public class ParkDb(DbContextOptions<ParkDb> options) : DbContext(options)
{
    public DbSet<Fence> Fences => Set<Fence>();
    public DbSet<Breach> Breaches => Set<Breach>();
    public DbSet<Paddock> Paddocks => Set<Paddock>();

    /// <summary>Every app in the solution shares one parkops.db next to ParkOps.slnx.</summary>
    public static string ConnectionString => $"Data Source={Path.Combine(FindSolutionRoot(), "parkops.db")}";

    public static ParkDb Create() =>
        new(new DbContextOptionsBuilder<ParkDb>().UseSqlite(ConnectionString).Options);

    /// <summary>Applies migrations and seeds an empty database.</summary>
    public static void Prepare(ParkDb db)
    {
        db.Database.Migrate();
        ParkSeeder.Seed(db);
    }

    private static string FindSolutionRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "ParkOps.slnx"))) return dir.FullName;
            }
        }
        return Directory.GetCurrentDirectory();
    }
}

/// <summary>Lets dotnet ef find the context without a startup project.</summary>
public class ParkDbDesignTimeFactory : IDesignTimeDbContextFactory<ParkDb>
{
    public ParkDb CreateDbContext(string[] args) => ParkDb.Create();
}
