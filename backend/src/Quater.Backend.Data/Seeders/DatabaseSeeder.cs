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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system"
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system"
        };

        // Get admin password from environment variable or generate a secure random one
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
        
        if (string.IsNullOrEmpty(adminPassword))
        {
            // Generate a secure random password if not provided
            adminPassword = GenerateSecurePassword();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("IMPORTANT: Default admin password generated!");
            Console.WriteLine($"Email: admin@quater.local");
            Console.WriteLine($"Password: {adminPassword}");
            Console.WriteLine("Please change this password immediately after first login.");
            Console.WriteLine("Set ADMIN_DEFAULT_PASSWORD environment variable to use a custom password.");
            Console.WriteLine("=".PadRight(80, '='));
        }
        
        var result = await userManager.CreateAsync(admin, adminPassword);
        
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    
    /// <summary>
    /// Generates a secure random password that meets the password requirements.
    /// </summary>
    private static string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        
        var random = new Random();
        var password = new char[16];
        
        // Ensure at least one of each required character type
        password[0] = uppercase[random.Next(uppercase.Length)];
        password[1] = lowercase[random.Next(lowercase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];
        
        // Fill the rest with random characters from all sets
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Shuffle the password to avoid predictable patterns
        return new string(password.OrderBy(x => random.Next()).ToArray());
    }
}
