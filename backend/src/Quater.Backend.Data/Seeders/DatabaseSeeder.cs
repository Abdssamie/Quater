using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Constants;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Data.Seeders;

/// <summary>
/// Seeds the database with initial data for Parameters and Admin user.
/// 
/// PRODUCTION SAFETY:
/// - In Production: ADMIN_DEFAULT_PASSWORD environment variable is REQUIRED
/// - In Development: Password is auto-generated if not provided (displayed in logs)
/// - Admin user is only created once; subsequent runs are skipped gracefully
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds all initial data into the database.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userManager">The user manager for creating admin user.</param>
    /// <param name="configuration">The configuration for environment check.</param>
    /// <param name="logger">The logger for output.</param>
    public static async Task SeedAsync(
        QuaterDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Admin User
        await SeedAdminUserAsync(context, userManager, configuration, logger);

        // Seed Parameters
        await SeedParametersAsync(context);
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
            new()
            {
                Id = Guid.NewGuid(),
                Name = "pH",
                Unit = "pH units",
                Description = "Measure of acidity or alkalinity of water",
                MinValue = 6.5,
                MaxValue = 8.5,
                IsActive = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Turbidity",
                Unit = "NTU",
                Description = "Cloudiness or haziness of water",
                MinValue = 0,
                MaxValue = 5,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Total Dissolved Solids",
                Unit = "mg/L",
                Description = "Total amount of dissolved substances in water",
                MinValue = 0,
                MaxValue = 500,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Chlorine Residual",
                Unit = "mg/L",
                Description = "Free chlorine remaining in water after treatment",
                MinValue = 0.2,
                MaxValue = 5.0,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Fluoride",
                Unit = "mg/L",
                Description = "Fluoride concentration in water",
                MinValue = 0,
                MaxValue = 1.5,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Nitrate",
                Unit = "mg/L",
                Description = "Nitrate concentration in water",
                MinValue = 0,
                MaxValue = 50,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Arsenic",
                Unit = "µg/L",
                Description = "Arsenic concentration in water",
                MinValue = 0,
                MaxValue = 10,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Lead",
                Unit = "µg/L",
                Description = "Lead concentration in water",
                MinValue = 0,
                MaxValue = 10,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "E. coli",
                Unit = "CFU/100mL",
                Description = "Escherichia coli bacteria count",
                MinValue = 0,
                MaxValue = 0,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Total Coliform",
                Unit = "CFU/100mL",
                Description = "Total coliform bacteria count",
                MinValue = 0,
                MaxValue = 0,
                IsActive = true
            }
        };

        await context.Parameters.AddRangeAsync(parameters);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the default admin user and lab.
    /// 
    /// PRODUCTION SAFETY:
    /// - In Production/Staging: ADMIN_DEFAULT_PASSWORD environment variable is REQUIRED
    ///   The application will fail to start if not provided, preventing auto-generated passwords
    /// - In Development: Password is auto-generated if not provided (displayed in logs)
    /// - Admin user is only created once; subsequent runs are skipped gracefully
    /// </summary>
    private static async Task SeedAdminUserAsync(
        QuaterDbContext context,
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Check if admin user already exists by ID
        var systemUserId = SystemUser.GetId();
        var adminUser = await userManager.FindByIdAsync(systemUserId.ToString());
        if (adminUser != null)
        {
            logger.LogDebug("Admin user already exists with ID {UserId}", systemUserId);
            return; // Admin already exists
        }

        // Also check if a user with the admin email already exists (handles ID changes)
        var existingAdminByEmail = await userManager.FindByEmailAsync("admin@quater.local");
        if (existingAdminByEmail != null)
        {
            logger.LogWarning(
                "Admin user already exists with email {Email} but different ID. " +
                "This may indicate SYSTEM_ADMIN_USER_ID was changed. Existing admin will be used.",
                "admin@quater.local");
            return; // Admin already exists with different ID
        }

        // Check environment
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
        var isStaging = environment.Equals("Staging", StringComparison.OrdinalIgnoreCase);

        // Get admin password from environment variable
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");

        if (string.IsNullOrEmpty(adminPassword))
        {
            if (isProduction || isStaging)
            {
                // In Production/Staging: FAIL FAST - require explicit password
                throw new InvalidOperationException(
                    "ADMIN_DEFAULT_PASSWORD environment variable is required in Production/Staging environments. " +
                    "Please set this variable to a secure password before starting the application. " +
                    "Example: ADMIN_DEFAULT_PASSWORD=YourSecurePassword123! dotnet run");
            }

            // In Development: Generate a secure random password
            adminPassword = GenerateSecurePassword();
            logger.LogWarning("=".PadRight(80, '='));
            logger.LogWarning("IMPORTANT: Default admin password generated!");
            logger.LogWarning("Email: admin@quater.local");
            logger.LogWarning("Password: {Password}", adminPassword);
            logger.LogWarning("Please change this password immediately after first login.");
            logger.LogWarning("Set ADMIN_DEFAULT_PASSWORD environment variable to use a custom password.");
            logger.LogWarning("=".PadRight(80, '='));
        }
        else
        {
            logger.LogInformation("Using ADMIN_DEFAULT_PASSWORD from environment variable");
        }

        // Create default lab first
        var defaultLab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Default Laboratory",
            Location = "Main Office",
            ContactInfo = "admin@quater.local",
            IsActive = true,
        };

        await context.Labs.AddAsync(defaultLab);

        // Create admin user
        var admin = new User
        {
            Id = systemUserId,
            UserName = "admin@quater.local",
            Email = "admin@quater.local",
            EmailConfirmed = true,
            IsActive = true,
            UserLabs =
            [
                new UserLab
                {
                    LabId = defaultLab.Id,
                    Role = UserRole.Admin
                }
            ]
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Admin user created successfully with ID {UserId}", systemUserId);
    }

    /// <summary>
    /// Generates a secure random password that meets the password requirements.
    /// Uses cryptographically secure random number generation.
    /// </summary>
    private static string GenerateSecurePassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        using var rng = RandomNumberGenerator.Create();
        var password = new char[16];

        // Ensure at least one of each required character type
        password[0] = uppercase[GetSecureRandomIndex(rng, uppercase.Length)];
        password[1] = lowercase[GetSecureRandomIndex(rng, lowercase.Length)];
        password[2] = digits[GetSecureRandomIndex(rng, digits.Length)];
        password[3] = special[GetSecureRandomIndex(rng, special.Length)];

        // Fill the rest with random characters from all sets
        var allChars = uppercase + lowercase + digits + special;
        for (var i = 4; i < password.Length; i++)
        {
            password[i] = allChars[GetSecureRandomIndex(rng, allChars.Length)];
        }

        // Shuffle the password to avoid predictable patterns using Fisher-Yates algorithm
        ShuffleArray(rng, password);

        return new string(password);
    }

    /// <summary>
    /// Gets a cryptographically secure random index for the given array length.
    /// </summary>
    private static int GetSecureRandomIndex(RandomNumberGenerator rng, int length)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var randomValue = BitConverter.ToInt32(bytes, 0) & int.MaxValue; // Ensure positive
        return randomValue % length;
    }

    /// <summary>
    /// Shuffles the array in place using Fisher-Yates algorithm with cryptographically secure random.
    /// </summary>
    private static void ShuffleArray(RandomNumberGenerator rng, char[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = GetSecureRandomIndex(rng, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
