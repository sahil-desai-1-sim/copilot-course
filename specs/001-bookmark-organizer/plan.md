# Implementation Plan: Bookmark Organizer

**Branch**: `001-bookmark-organizer` | **Date**: 2026-05-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-bookmark-organizer/spec.md`

## Summary

A cross-platform CLI tool that lets users save, tag, search, and manage bookmarks locally with zero network
dependency. Built on C# / .NET 10, using `System.CommandLine` for the CLI layer and a local SQLite database
(with FTS5 full-text search) for persistence. The architecture isolates business logic in a Services layer,
data access behind a repository interface, and CLI command handling as thin wrappers — enabling ≥ 80 %
unit test coverage and fast integration tests against every exposed command contract.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: `System.CommandLine` (CLI parsing), `Microsoft.Data.Sqlite` (storage), `xUnit` + `FluentAssertions` + `Moq` (tests), `BenchmarkDotNet` (performance benchmarks)  
**Storage**: SQLite (single-file local database) with FTS5 virtual table for full-text search  
**Testing**: xUnit, FluentAssertions, Moq; coverage via `dotnet-coverage` / Coverlet; benchmarks via BenchmarkDotNet  
**Target Platform**: Cross-platform — Windows, macOS, Linux (.NET 10 runtime or self-contained publish)  
**Project Type**: CLI application  
**Performance Goals**: Standard ops (add, list, tag, delete, update) < 500 ms; keyword search < 1 s — both at 10,000 bookmarks  
**Constraints**: Offline-capable; no network calls at runtime; peak memory < 50 MB for standard collections; single-user store  
**Scale/Scope**: Single user; designed to scale to 50,000+ bookmarks without degradation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with all four Core Principles before proceeding:

- [x] **I. Code Quality** — All public types documented with XML doc comments; business logic split across small, focused service classes (SRP); no magic strings (constants class); no dead code paths.
- [x] **II. Testing Standards** — Unit tests for all service and repository methods target ≥ 80 % coverage; every CLI command has an integration test; test names follow `Method_GivenState_ExpectedOutcome` convention.
- [x] **III. User Experience Consistency** — All commands follow `bookmark <verb> [noun] [options]` structure; all errors include what/why/how; `--json` flag supported on list/search/export; help text generated automatically by `System.CommandLine`.
- [x] **IV. Performance Requirements** — SQLite with FTS5 and appropriate indexes satisfies all performance targets; BenchmarkDotNet benchmarks defined for add/search/list on 10 k rows; no LINQ-to-objects full-table scans in hot paths.

## Project Structure

### Documentation (this feature)

```text
specs/001-bookmark-organizer/
├── plan.md              # This file
├── research.md          # Phase 0: technology decisions and rationale
├── data-model.md        # Phase 1: entity definitions, relationships, validation rules
├── quickstart.md        # Phase 1: how to build, run, and test the application
├── contracts/           # Phase 1: CLI command contracts
│   ├── add.md
│   ├── list.md
│   ├── search.md
│   ├── tag.md
│   ├── update.md
│   ├── delete.md
│   ├── export.md
│   └── import.md
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
BookmarkOrganizer/
├── BookmarkOrganizer.csproj
├── Program.cs                         # Entry point — builds DI container, wires commands
├── Commands/
│   ├── AddCommand.cs
│   ├── ListCommand.cs
│   ├── SearchCommand.cs
│   ├── TagCommand.cs
│   ├── UpdateCommand.cs
│   ├── DeleteCommand.cs
│   ├── ExportCommand.cs
│   └── ImportCommand.cs
├── Models/
│   ├── Bookmark.cs
│   └── Tag.cs
├── Services/
│   ├── IBookmarkService.cs
│   ├── BookmarkService.cs
│   ├── ISearchService.cs
│   ├── SearchService.cs
│   ├── IImportExportService.cs
│   └── ImportExportService.cs
├── Infrastructure/
│   ├── IBookmarkRepository.cs
│   ├── SqliteBookmarkRepository.cs
│   ├── DatabaseInitializer.cs
│   └── MigrationRunner.cs
└── Constants.cs                       # All string/numeric constants

BookmarkOrganizer.Tests/
├── BookmarkOrganizer.Tests.csproj
├── Unit/
│   ├── Services/
│   │   ├── BookmarkServiceTests.cs
│   │   ├── SearchServiceTests.cs
│   │   └── ImportExportServiceTests.cs
│   └── Models/
│       └── BookmarkTests.cs
├── Integration/
│   ├── Commands/
│   │   ├── AddCommandTests.cs
│   │   ├── ListCommandTests.cs
│   │   ├── SearchCommandTests.cs
│   │   ├── TagCommandTests.cs
│   │   ├── UpdateCommandTests.cs
│   │   ├── DeleteCommandTests.cs
│   │   ├── ExportCommandTests.cs
│   │   └── ImportCommandTests.cs
│   └── Infrastructure/
│       └── SqliteBookmarkRepositoryTests.cs
└── Benchmarks/
    └── BookmarkBenchmarks.cs          # BenchmarkDotNet — add/list/search at 10k rows
```

**Structure Decision**: Single-project layout (Option 1 variant). The CLI application and its business
logic live in `BookmarkOrganizer/`. A separate `BookmarkOrganizer.Tests/` project contains all xUnit
unit, integration, and benchmark tests. The existing `ConsoleApp/` project in the solution is retained
as-is; `BookmarkOrganizer/` is added as a new project in `copilot-course.sln`.

## Complexity Tracking

> No constitution violations. All design decisions satisfy the Core Principles without justified exceptions.
