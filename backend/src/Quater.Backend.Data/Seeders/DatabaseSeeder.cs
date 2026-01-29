using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Seeders;

/// <summary>
/// Seeds the database with initial data for Parameters and Admin user.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds all initial data into the database.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userManager">The user manager for creating admin user.</param>
    public static async Task SeedAsync(QuaterDbContext context, UserManager<User> userManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Parameters
        await SeedParametersAsync(context);

        // Seed Admin User
        await SeedAdminUserAsync(context, userManager);
    }

    /// <summary>
    /// Seeds standard water quality parameters.
    /// </summary>
    private static async Task SeedParametersAsync(QuaterDbContext context)
    {
        // Check if parameters already exist
        if (await context.Parameters.AnyAsync())
        {
            return; // Parameters already seeded
        }

        var parameters = new List<Parameter>
        {
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "pH",
                Unit = "pH units",
                Description = "Measure of acidity or alkalinity of water",
                MinValue = 6.5,
                MaxValue = 8.5,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Turbidity",
                Unit = "NTU",
                Description = "Cloudiness or haziness of water",
                MinValue = 0,
                MaxValue = 5,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Total Dissolved Solids",
                Unit = "mg/L",
                Description = "Total amount of dissolved substances in water",
                MinValue = 0,
                MaxValue = 500,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Chlorine Residual",
                Unit = "mg/L",
                Description = "Free chlorine remaining in water after treatment",
                MinValue = 0.2,
                MaxValue = 5.0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Fluoride",
                Unit = "mg/L",
                Description = "Fluoride concentration in water",
                MinValue = 0,
                MaxValue = 1.5,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Nitrate",
                Unit = "mg/L",
                Description = "Nitrate concentration in water",
                MinValue = 0,
                MaxValue = 50,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Arsenic",
                Unit = "µg/L",
                Description = "Arsenic concentration in water",
                MinValue = 0,
                MaxValue = 10,
          IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Lead",
                Unit = "µg/L",
                Description = "Lead concentration in water",
                MinValue = 0,
                MaxValue = 10,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "E. coli",
                Unit = "CFU/100mL",
                Description = "Escherichia coli bacteria count",
                MinValue = 0,
                MaxValue = 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Total Coliform",
                Unit = "CFU/100mL",
                Description = "Total coliform bacteria count",
                MinValue = 0,
                MaxValue = 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }
        };

        await context.Parameters.AddRangeAsync(parameters);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the default admin user and lab.
    /// </summary>
    private static async Task SeedAdminUserAsync(QuaterDbContext context, UserManager<User> userManager)
    {
        // Check if admin user already exists
        var adminUser = await userManager.FindByEmailAsync("admin@quater.local");
        if (adminUser != null)
        {
            return; // Admin already exists
        }

        // Create default lab first
        var defaultLab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Default Laboratory",
            Location = "Main Office",
            ContactInfo = "admin@quater.local",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await context.Labs.AddAsync(defaultLab);
        await context.SaveChangesAsync();

        // Create admin user
        var admin = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@quater.local",
            Email = "admin@quater.local",
            EmailConfirmed = true,
            Role = UserRole.Admin,
            LabId = defaultLab.Id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        // Create user with default password
        var result = await userManager.CreateAsync(admin, "Admin@123");
        
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
