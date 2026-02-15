using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Quater.Admin.Cli.Infrastructure;
using Quater.Admin.Cli.Services;
using Quater.Admin.Cli.Utilities;

namespace Quater.Admin.Cli.Commands;

/// <summary>
/// Command handler for resetting user passwords.
/// Pure business logic - no parsing concerns.
/// </summary>
public static class ResetPasswordCommand
{
    public static Command Create()
    {
        var emailOption = new Option<string>(name: "--email", aliases: ["-e"])
        {
            Description = "Email of user to reset",
            DefaultValueFactory = _ => "admin@quater.local"
        };

        var passwordOption = new Option<string?>(
            name: "--password",
            aliases: ["-p"])
        {
            Description = "New password (prompted if not provided)"
        };

        var command = new Command("reset-password", "Reset a user's password")
        {
            emailOption,
            passwordOption
        };
        
        command.SetAction(parseResult =>
        {
            var email = parseResult.GetValue(emailOption);
            if (email is null) throw new InvalidOperationException("Email not found");
            
            var password = parseResult.GetValue(passwordOption);
            
            return ExecuteAsync(email, password);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string email,
        string? password,
        CancellationToken ct = default)
    {
        try
        {
            // Prompt for password if not provided
            password ??= ConsoleHelper.ReadPasswordWithConfirmation(email);

            // Execute business logic
            await using var serviceProvider = ServiceProviderFactory.BuildServiceProvider();
            var userService = serviceProvider.GetRequiredService<UserManagementService>();

            var result = await userService.ResetPasswordAsync(email, password, ct);

            if (!result.Succeeded)
            {
                ConsoleHelper.WriteError("Failed to reset password:");
                foreach (var error in result.Errors)
                {
                    ConsoleHelper.WriteError($"  - {error.Description}");
                }
                
                return 1;
            }

            ConsoleHelper.WriteSuccess("Password reset successfully!");
            ConsoleHelper.WriteInfo($"Email: {email}");

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleHelper.WriteError(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}