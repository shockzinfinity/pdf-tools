using System.CommandLine;
using PdfTool.Commands;

namespace PdfTool;

/// <summary>
/// Entry point. Responsibility is strictly limited to:
///   1. Creating the RootCommand.
///   2. Registering subcommands (Command pattern via ICliCommand).
///   3. Delegating parsing + execution to System.CommandLine.
/// New features (merge, encrypt, ...) plug in by adding one line below.
/// </summary>
public static class Program
{
  public static async Task<int> Main(string[] args)
  {
    var root = new RootCommand("pdf-tool — a .NET Global Tool for PDF editing.");

    // Register subcommands here. Each ICliCommand encapsulates its own
    // arguments, options, and handler — Program.cs stays thin.
    ICliCommand[] commands =
    [
      new SeparateCommand()
      // new MergeCommand(),
      // new EncryptCommand(),
    ];

    foreach (var c in commands)
      root.AddCommand(c.Build());

    return await root.InvokeAsync(args);
  }
}
