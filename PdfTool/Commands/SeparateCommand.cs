using System.CommandLine;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfTool.Commands;

/// <summary>
/// `pdf-tool separate &lt;file&gt; --output &lt;dir&gt; --pages &lt;range&gt;`
///
/// Splits a PDF into individual single-page files. If --pages is omitted every
/// page is extracted. Output files are named {originalName}_page_{n}.pdf.
/// </summary>
public sealed class SeparateCommand : ICliCommand
{
  public Command Build()
  {
    var fileArg = new Argument<FileInfo>("file")
    {
      Description = "Path to the source PDF file.",
    };

    var outputOption = new Option<DirectoryInfo?>("--output", "-o")
    {
      Description = "Output directory. Defaults to the source file's directory.",
    };

    var pagesOption = new Option<string?>("--pages", "-p")
    {
      Description = "Page selection, e.g. '1-3,5,8-10'. Omit to extract every page.",
    };

    var cmd = new Command("separate", "Split a PDF into individual single-page files.")
    {
      fileArg,
      outputOption,
      pagesOption,
    };

    cmd.SetAction(parseResult =>
    {
      var file = parseResult.GetValue(fileArg)!;
      var output = parseResult.GetValue(outputOption);
      var pages = parseResult.GetValue(pagesOption);
      return Execute(file, output, pages);
    });

    return cmd;
  }

  private static int Execute(FileInfo file, DirectoryInfo? output, string? pagesExpression)
  {
    // 1. Validation — existence
    if (!file.Exists)
    {
      Console.Error.WriteLine($"error: file not found: {file.FullName}");
      return 2;
    }

    // 1b. Validation — extension check (fast sanity check)
    if (!string.Equals(file.Extension, ".pdf", StringComparison.OrdinalIgnoreCase))
    {
      Console.Error.WriteLine($"error: not a .pdf file: {file.Name}");
      return 2;
    }

    // 2. Resolve output directory
    var outDir = output ?? new DirectoryInfo(file.DirectoryName ?? Environment.CurrentDirectory);
    if (!outDir.Exists)
    {
      try
      { outDir.Create(); }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"error: cannot create output dir '{outDir.FullName}': {ex.Message}");
        return 3;
      }
    }

    // 3. Open source PDF — this also validates the PDF format
    PdfDocument source;
    try
    {
      source = PdfReader.Open(file.FullName, PdfDocumentOpenMode.Import);
    }
    catch (PdfReaderException ex)
    {
      Console.Error.WriteLine($"error: invalid PDF format: {ex.Message}");
      return 4;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"error: cannot open PDF: {ex.Message}");
      return 4;
    }

    using (source)
    {
      var total = source.PageCount;
      if (total == 0)
      {
        Console.Error.WriteLine("error: source PDF contains no pages.");
        return 5;
      }

      // 4. Parse page selection
      IReadOnlyList<int> pages;
      try
      {
        pages = PageRangeParser.Parse(pagesExpression, total);
      }
      catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
      {
        Console.Error.WriteLine($"error: invalid --pages value: {ex.Message}");
        return 6;
      }

      // 5. Extract each selected page into its own single-page document
      var baseName = Path.GetFileNameWithoutExtension(file.Name);
      var written = 0;

      foreach (var pageNumber in pages)
      {
        using var output1 = new PdfDocument();
        output1.Version = source.Version;
        output1.AddPage(source.Pages[pageNumber - 1]); // 1-based → 0-based

        var outPath = Path.Combine(outDir.FullName, $"{baseName}_page_{pageNumber}.pdf");
        try
        {
          output1.Save(outPath);
          written++;
          Console.WriteLine($"wrote {outPath}");
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine($"error: failed to write '{outPath}': {ex.Message}");
          return 7;
        }
      }

      Console.WriteLine($"done: {written} file(s) written to {outDir.FullName}");
    }

    return 0;
  }
}
