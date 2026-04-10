# pdf-tool

A cross-platform .NET Global Tool for PDF editing — starts with **separate** (split a PDF
into single-page files) and is designed to grow into **merge**, **encrypt**, and more
without touching existing code.

- **Target framework**: .NET 10
- **CLI framework**: [System.CommandLine 2.0](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) (GA / stable)
- **PDF engine**: [PdfSharp 6.2](https://www.pdfsharp.net/) (MIT licensed)
- **Architecture**: Command pattern via `ICliCommand` — one class per subcommand.

---

## Install

### From a local build

```bash
# 1. Build & pack
dotnet pack pdf-tools.slnx -c Release

# 2. Install globally (from the solution root)
dotnet tool install --global PdfTool \
    --add-source ./nupkg --version 0.2.0
```

### Update / Uninstall

```bash
dotnet tool update    --global PdfTool --add-source ./nupkg
dotnet tool uninstall --global PdfTool
```

> `~/.dotnet/tools` must be on your `PATH` (the dotnet installer usually adds it).

---

## Usage

```
pdf-tool separate <file> [--output <dir>] [--pages <range>]
```

| Flag | Alias | Description |
|---|---|---|
| `<file>` | — | Path to the source PDF. **Required.** |
| `--output` | `-o` | Output directory. Defaults to the source file's directory. |
| `--pages` | `-p` | Page selection (e.g. `1-3,5,8-10`). Omit to extract every page. |

Output file naming: `{originalName}_page_{n}.pdf`

### Examples

```bash
# Split every page into its own file (next to the source)
pdf-tool separate report.pdf

# Into a specific directory
pdf-tool separate report.pdf --output ./split

# Extract pages 1, 2, 3 and 5 only
pdf-tool separate report.pdf -o ./split -p "1-3,5"

# Extract a single page
pdf-tool separate report.pdf -p "7"
```

### Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `2` | File not found / not a `.pdf` |
| `3` | Output directory cannot be created |
| `4` | Invalid PDF format / unreadable |
| `5` | Source PDF has zero pages |
| `6` | `--pages` range is invalid |
| `7` | Write failure |

---

## Development

### Prerequisites

- .NET **10 SDK** or newer (`dotnet --version` ≥ 10.0)

### Build & run from source

```bash
# Restore + build
dotnet build pdf-tools.slnx -c Release

# Run without installing
dotnet run --project PdfTool -- separate sample.pdf -o out
```

### Project layout

```
pdf-tools/
├── pdf-tools.slnx              # Solution (.NET 9+ XML format)
├── .editorconfig               # Shared coding conventions
├── .gitignore                  # dotnet build artifacts
├── .gitattributes              # Cross-platform line endings
├── README.md
├── CLAUDE.md                   # Claude Code collaboration guide
├── nupkg/                      # Built .nupkg output
└── PdfTool/
    ├── PdfTool.csproj          # Global Tool metadata
    ├── Program.cs              # RootCommand registration only
    └── Commands/
        ├── ICliCommand.cs      # Command-pattern contract
        ├── PageRangeParser.cs  # "1-3,5" → [1,2,3,5]
        └── SeparateCommand.cs  # `separate` implementation
```

### Adding a new subcommand

1. Create `PdfTool/Commands/{Name}Command.cs` implementing `ICliCommand`.
2. Register it in `PdfTool/Program.cs`:

   ```csharp
   ICliCommand[] commands =
   {
       new SeparateCommand(),
       new MergeCommand(),   // ← add here
   };
   ```

That's it — `Program.cs` doesn't need to know about arguments or options.

---

## Cross-platform support

`pdf-tool` targets .NET 10 and works on **Windows, macOS, and Linux** (x64 and arm64).

Design choices that keep it portable:

- All paths use `System.IO.Path.Combine` and `FileInfo` / `DirectoryInfo` — no
  hard-coded separators.
- No P/Invoke, no OS-specific APIs.
- Line endings are normalized through `.gitattributes` / `.editorconfig` so the
  same source compiles identically on Windows (CRLF) and Unix (LF).

### Known caveat

- **PdfSharp font resolver on non-Windows**: PdfSharp's `XFont` needs a font
  resolver on Linux/macOS (it looks for Windows system fonts by default). This
  tool's **`separate` command does not render text**, so it is unaffected.
  Future commands that *draw* text (e.g. watermarking) will need to register
  [`GlobalFontSettings.FontResolver`](https://docs.pdfsharp.net/) explicitly.

---

## Roadmap

- [x] `separate` — split PDF into single-page files
- [ ] `merge` — combine multiple PDFs into one
- [ ] `encrypt` — password-protect a PDF (owner/user passwords)
- [ ] `rotate` — rotate selected pages
- [ ] `extract-text` — dump text content

---

## License

MIT. PdfSharp is MIT licensed. System.CommandLine is MIT licensed.
