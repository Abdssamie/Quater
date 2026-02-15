namespace Quater.Admin.Cli.Utilities;

/// <summary>
/// Reusable console utilities for interactive prompts.
/// </summary>
public static class ConsoleHelper
{
    private static string ReadPassword(string prompt = "Password: ")
    {
        Console.Write(prompt);
        var password = string.Empty;
        ConsoleKey key;

        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                Console.Write("\b \b");
                password = password[..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                password += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }

    public static string ReadPasswordWithConfirmation(string email)
    {
        var password = ReadPassword($"Enter new password for {email}: ");
        var confirmPassword = ReadPassword("Confirm password: ");

        if (password != confirmPassword)
        {
            throw new InvalidOperationException("Passwords do not match!");
        }

        return password;
    }

    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }
}
