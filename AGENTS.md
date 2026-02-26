# Repository Guidelines

## Project Structure & Module Organization
`ArbSh` is a .NET 9 solution centered in `src_csharp/ArbSh.sln`:
- `src_csharp/ArbSh.Core`: shell engine (parser, executor, cmdlets, BiDi/i18n, session state).
- `src_csharp/ArbSh.Console`: console host.
- `src_csharp/ArbSh.Terminal`: Avalonia GUI terminal host and rendering/input pipeline.
- `src_csharp/ArbSh.Test`: xUnit tests.
- `docs/`: roadmap, usage, architecture notes, changelog.
- `installer/`: Windows install/uninstall scripts (Explorer context-menu support).
- `create-release.ps1`: release/installer packaging automation.

## Build, Test, and Development Commands
- `dotnet build src_csharp/ArbSh.sln`  
  Builds all projects.
- `dotnet run --project src_csharp/ArbSh.Console`  
  Runs console REPL host.
- `dotnet run --project src_csharp/ArbSh.Terminal`  
  Runs Avalonia GUI terminal.
- `dotnet test src_csharp/ArbSh.Test/ArbSh.Test.csproj`  
  Runs xUnit tests.
- `.\create-release.ps1 -Version "0.8.1-alpha"`  
  Produces release zip.
- `.\create-release.ps1 -Version "0.8.1-alpha" -CreateInstaller`  
  Produces release + installer package zip.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: 4 spaces, CRLF, UTF-8.
- C# conventions: `PascalCase` for types/methods, `_camelCase` for fields.
- Keep nullable-safe patterns (`<Nullable>enable</Nullable>` is active).
- User-facing commands/parameters must use `[ArabicName("...")]`.
- Public APIs should include XML docs; for shell-facing behavior, prefer Arabic summaries/messages.
- Preserve logical/visual separation: parsing/state in logical order; display shaping/reordering only at UI/render boundaries.

## Testing Guidelines
- Framework: xUnit (`ArbSh.Test`).
- Place tests in `*Tests.cs`; use clear behavior names (e.g., `Action_State_Expected`).
- Add tests for parser/binding/cmdlets and BiDi-sensitive behavior when applicable.
- For file-system features, use isolated temp directories and clean up after tests.

## Commit & Pull Request Guidelines
- Current history favors concise imperative subjects: `Add ...`, `Refactor ...`, `Fix ...`.
- Keep commits scoped to one logical change.
- PRs should include:
  - What changed and why.
  - Linked issue/roadmap item.
  - Test evidence (`dotnet test` output).
  - UI screenshots/GIFs for terminal rendering/input changes.
  - Documentation updates (`docs/USAGE_EXAMPLES.md`, `docs/CHANGELOG.md`, `ROADMAP.md`) when behavior changes.
