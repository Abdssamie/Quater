using System.CommandLine;
using Quater.Admin.Cli.Commands;

namespace Quater.Admin.Cli;

/// <summary>
/// Quater Admin CLI Tool - Clean, declarative command-line interface.
/// Business logic is decoupled in Services/ - this is just the thin CLI layer.
/// </summary>
class Program
{
    static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Quater Admin CLI - User management and administrative tasks")
        {
            ResetPasswordCommand.Create(),
        };

        ParseResult parseResult = rootCommand.Parse(args);
        
        return parseResult.InvokeAsync();
    }
}