using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Quater.Desktop.Data;

/// <summary>
/// Design-time factory for QuaterLocalContext.
/// Used by EF Core tools for migrations.
/// </summary>
public class QuaterLocalContextFactory : IDesignTimeDbContextFactory<QuaterLocalContext>
{
    public QuaterLocalContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuaterLocalContext>();
        optionsBuilder.UseSqlite("Data Source=quater.db");

        return new QuaterLocalContext(optionsBuilder.Options);
    }
}
