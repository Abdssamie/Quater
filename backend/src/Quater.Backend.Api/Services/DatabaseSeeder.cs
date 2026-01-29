using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Enums;
using Quater.Backend.Core.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Api.Services;

/// <summary>
/// Service for seeding initial database data
/// </summary>
public class DatabaseSeeder
{
    private readonly QuaterDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(
        QuaterDbContext context,
        UserManager<User> userManager,
        ILogger<DatabaseSeeder> logger,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _timeProvider = timeProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Seed initial data (lab, admin user, parameters)
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Seed default lab
            var lab = await SeedDefaultLabAsync();

            // Seed default admin user
            await SeedDefaultAdminAsync(lab.Id);

            // Seed default parameters
            await SeedDefaultParametersAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task<Lab> SeedDefaultLabAsync()
    {
        var existingLab = await _context.Labs.FirstOrDefaultAsync();
        if (existingLab != null)
        {
            _logger.LogInformation("Default lab already exists");
            return existingLab;
        }

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = "Default Lab",
            Location = "Default Location",
            ContactInfo = "contact@quater.local",
            CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
            IsActive = true
        };

        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Default lab created with ID: {LabId}", lab.Id);
        return lab;
    }

    private async Task SeedDefaultAdminAsync(Guid labId)
    {
        const string adminEmail = "admin@quater.local";
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin@123";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Default admin user already exists");
            return;
        }

        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Role = UserRole.Admin,
            LabId = labId,
            CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Default admin user created: {Email}", adminEmail);
            _logger.LogWarning("SECURITY: Default admin password is '{Password}'. Change this immediately in production!", adminPassword);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create default admin user: {Errors}", errors);
        }
    }

    private async Task SeedDefaultParametersAsync()
    {
        if (await _context.Parameters.AnyAsync())
        {
            _logger.LogInformation("Parameters already seeded");
            return;
        }

        var parameters = new[]
        {
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "pH",
                Unit = "pH",
                WhoThreshold = 8.5,
                MinValue = 6.5,
                MaxValue = 8.5,
                Description = "Measure of acidity or alkalinity",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Turbidity",
                Unit = "NTU",
                WhoThreshold = 5,
                MinValue = 0,
                MaxValue = 1000,
                Description = "Cloudiness or haziness of water",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Free Chlorine",
                Unit = "mg/L",
                WhoThreshold = 5,
                MinValue = 0.2,
                MaxValue = 5,
                Description = "Free chlorine residual",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Total Chlorine",
                Unit = "mg/L",
                WhoThreshold = 5,
                MinValue = 0,
                MaxValue = 10,
                Description = "Total chlorine (free + combined)",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "E. coli",
                Unit = "CFU/100mL",
                WhoThreshold = 0,
                MinValue = 0,
                MaxValue = 1000,
                Description = "Escherichia coli bacteria count",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Total Coliforms",
                Unit = "CFU/100mL",
                WhoThreshold = 0,
                MinValue = 0,
                MaxValue = 1000,
                Description = "Total coliform bacteria count",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Temperature",
                Unit = "°C",
                MinValue = 0,
                MaxValue = 100,
                Description = "Water temperature",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Conductivity",
                Unit = "µS/cm",
                MinValue = 0,
                MaxValue = 10000,
                Description = "Electrical conductivity",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Dissolved Oxygen",
                Unit = "mg/L",
                MinValue = 0,
                MaxValue = 20,
                Description = "Amount of oxygen dissolved in water",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            },
            new Parameter
            {
                Id = Guid.NewGuid(),
                Name = "Hardness",
                Unit = "mg/L CaCO3",
                MinValue = 0,
                MaxValue = 1000,
                Description = "Total hardness as calcium carbonate",
                IsActive = true,
                CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                LastModified = _timeProvider.GetUtcNow().UtcDateTime
            }
        };

        _context.Parameters.AddRange(parameters);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} default parameters", parameters.Length);
    }
}
