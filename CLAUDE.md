# CLAUDE.md

Collaboration guide for Claude Code working on **pdf-tool** — a .NET 8 Global Tool
for PDF editing. This file is loaded automatically by Claude Code on every
session in this repository.

---

## Project snapshot

- **Language / runtime**: C# 12 on .NET 8 (LTS). Target framework is pinned in
  `PdfTool/PdfTool.csproj` (`<TargetFramework>net8.0</TargetFramework>`).
- **CLI framework**: `System.CommandLine` **2.0.0-beta4.22272.1**. Newer betas
  have breaking API changes — **do not upgrade casually**.
- **PDF engine**: `PdfSharp` **6.1.1** (MIT). Avoid iText7 (AGPL) and QuestPDF
  (generation-only).
- **Packaging**: distributed as a .NET Global Tool
  (`<PackAsTool>true</PackAsTool>`, `<ToolCommandName>pdf-tool</ToolCommandName>`).
- **Solution format**: `.slnx` (XML-based, .NET 9+ tooling).

## Repository layout

```
pdf-tools/
├── pdf-tools.slnx              # Solution
├── .editorconfig               # Style & analyzer rules (2-space, file-scoped ns)
├── .gitignore / .gitattributes
├── README.md                   # User-facing docs
├── CLAUDE.md                   # This file
├── nupkg/                      # `dotnet pack` output (solution-root)
└── PdfTool/
    ├── PdfTool.csproj
    ├── Program.cs              # Thin entry point — registers ICliCommands
    └── Commands/
        ├── ICliCommand.cs      # Command-pattern contract
        ├── PageRangeParser.cs  # "1-3,5" → [1,2,3,5]
        └── SeparateCommand.cs
```

## Build / run / test commands

Always run these from the **solution root** (`pdf-tools/`):

| Action | Command |
|---|---|
| Restore | `dotnet restore pdf-tools.slnx` |
| Build (Release) | `dotnet build pdf-tools.slnx -c Release` |
| Run without installing | `dotnet run --project PdfTool -- separate sample.pdf -o out` |
| Pack (.nupkg) | `dotnet pack pdf-tools.slnx -c Release` |
| Install locally | `dotnet tool install --global PdfTool --add-source ./nupkg --version 0.1.0` |
| Reinstall after code change | `dotnet tool update --global PdfTool --add-source ./nupkg` |
| Format check (CI-friendly) | `dotnet format pdf-tools.slnx --verify-no-changes` |
| Auto-format | `dotnet format pdf-tools.slnx` |

> There are no unit tests yet. When you add logic that deserves tests (the
> `PageRangeParser` is the obvious candidate), create a `PdfTool.Tests/` project
> next to `PdfTool/`, reference it from `pdf-tools.slnx`, and use **xUnit**.

## Architecture

### Command pattern via `ICliCommand`

`Program.cs` is intentionally dumb: it only creates a `RootCommand` and iterates
an array of `ICliCommand` instances, calling `.Build()` on each.

```csharp
ICliCommand[] commands =
{
  new SeparateCommand(),
  // new MergeCommand(),
};
foreach (var c in commands) root.AddCommand(c.Build());
```

Every subcommand lives in its own file under `PdfTool/Commands/` and owns
its arguments, options, and handler. **This is the only place Claude should
add new CLI behavior.**

### Adding a new subcommand

1. Create `PdfTool/Commands/{Name}Command.cs`.
2. Implement `ICliCommand` — `Build()` returns a `System.CommandLine.Command`.
3. Extract option alias arrays to `private static readonly string[]` fields
   (analyzer rule **CA1861**).
4. Add `new {Name}Command()` to the array in `Program.cs`.
5. Run `dotnet build` — warnings are treated as important; keep the build at
   **0 warnings, 0 errors**.
6. Update the Roadmap section of `README.md`.

### Exit code contract

`SeparateCommand` uses these codes. Reuse/extend consistently when adding
commands:

| Code | Meaning |
|---|---|
| 0 | Success |
| 2 | Input validation (missing file / wrong extension) |
| 3 | Output directory problem |
| 4 | PDF parse failure |
| 5 | Empty document |
| 6 | `--pages` / range argument invalid |
| 7 | Write failure |

## Coding conventions (enforced via `.editorconfig`)

- **2-space indent**, spaces only (no tabs).
- **File-scoped namespaces** (`namespace X;`) — enforced as `warning` via
  `csharp_style_namespace_declarations = file_scoped:warning` and IDE0161.
- **`using` directives outside the namespace**.
- **`var` everywhere** it's reasonable.
- **Nullable reference types enabled** — respect the annotations.
- **No multiple blank lines** and no dead code (IDE0051/IDE0052 at warning).
- Line width ≈ 120. Not hard-enforced, but don't go wild.

`EnforceCodeStyleInBuild=true` in the .csproj means any editorconfig rule at
`warning` severity fails CI. Always run `dotnet build` before declaring a task
done.

## Cross-platform considerations

This tool runs on **Windows, macOS, and Linux**.

- Use `Path.Combine` / `FileInfo` / `DirectoryInfo` — **never** hard-code `\`
  or `/`.
- Do not assume case-sensitive or case-insensitive filesystems.
- **PdfSharp font resolver trap**: on Linux/macOS, `new XFont("Arial", ...)`
  throws unless a font resolver is registered (PdfSharp looks for Windows
  system fonts by default). `separate` does not render text so it's safe,
  but any future draw/watermark command must configure
  `GlobalFontSettings.FontResolver` explicitly.
- Line endings are normalized via `.gitattributes`: repo-internal files are
  LF; `.sln` / `.cmd` / `.bat` / `.ps1` stay CRLF. `.editorconfig` enforces
  the same on save.

## Git hygiene

- `.gitignore` excludes `bin/`, `obj/`, `nupkg/`, OS files, and IDE state.
- `.gitattributes` enforces LF for source and binary-safe handling for
  `.pdf`, `.dll`, `.nupkg`.
- PDFs in the repo are flagged `linguist-generated=true` so they don't
  pollute GitHub language stats.
- Never commit the contents of `nupkg/` — it's generated output.

## Gotchas

- **System.CommandLine beta churn**: stick with beta4. API differences
  between beta4 and newer betas are non-trivial (`SetHandler` vs.
  `SetAction`, binder changes, etc.).
- **PdfSharp `PdfDocumentOpenMode`**: use `Import` when splitting so pages
  can be copied into a new `PdfDocument`. `Modify` will not let you attach
  pages to a second document.
- **`PackageOutputPath` is resolved relative to the `.csproj`**, so the
  current setting `$(MSBuildThisFileDirectory)../nupkg` puts the package at
  the solution root regardless of where `dotnet pack` is invoked.
- **`dotnet format` respects only `.editorconfig`** — no separate config
  file. Any style tweak goes in `.editorconfig`, never in project files.

## What Claude should NOT do here

- Don't add unit-test frameworks or test projects unless asked — the repo
  is still scaffolding.
- Don't upgrade `System.CommandLine` past beta4.
- Don't introduce iText7 or QuestPDF as alternatives. PdfSharp is the
  settled choice for licensing and capability reasons.
- Don't commit generated PDFs, `nupkg/` content, or `bin/obj/` output.
- Don't switch the indent width, brace style, or namespace style without
  updating `.editorconfig` in the same change.
