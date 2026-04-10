using System.CommandLine;

namespace PdfTool.Commands;

/// <summary>
/// Command pattern contract for each pdf-tool subcommand.
/// Each implementation builds its own System.CommandLine Command
/// (name, arguments, options, handler) and exposes it via Build().
/// Program.cs only needs to know about this interface to register new commands.
/// </summary>
public interface ICliCommand
{
  Command Build();
}
