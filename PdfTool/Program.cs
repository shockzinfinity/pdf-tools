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
  public static int Main(string[] args)
  {
    var root = new RootCommand("pdf-tool — a .NET Global Tool for PDF editing.");

    // Register subcommands here. Each ICliCommand encapsulates its own
    // arguments, options, and handler — Program.cs stays thin.
    ICliCommand[] commands =
    [
      new SeparateCommand(),
      // new MergeCommand(),
      // new EncryptCommand(),
    ];

    foreach (var c in commands)
      root.Subcommands.Add(c.Build());

    return root.Parse(args).Invoke();
  }
}
