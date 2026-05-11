# Research: Bookmark Organizer

**Phase 0 output** | **Date**: 2026-05-11 | **Plan**: [plan.md](plan.md)

All NEEDS CLARIFICATION items from the Technical Context have been resolved below.

---

## 1. CLI Framework

**Decision**: `System.CommandLine` (Microsoft, latest stable release targeting .NET 10)

**Rationale**:
- First-party Microsoft library; actively maintained and aligned with .NET release cadence.
- Native support for nested subcommands that cleanly map to `<verb> <noun> [options]` (Constitution III).
- Automatic `--help` generation for every command and option — satisfies FR-013 / FR-015.
- Supports tab completion, `--version`, and `--json` output routing without additional dependencies.
- Zero extra transitive dependencies beyond the core NuGet package.

**Alternatives considered**:
- **Spectre.Console.Cli**: Rich terminal UI, but adds significant dependency weight; the application does
  not require colour output or progress bars for standard ops.
- **Cocona**: Simpler attribute-based API, but less control over help formatting and output routing; less
  actively maintained.
- **Manual arg parsing**: Rejected — brittle, requires reinventing help text, flag parsing, and error
  messages that System.CommandLine provides for free.

---

## 2. Local Storage Format

**Decision**: SQLite via `Microsoft.Data.Sqlite` with the FTS5 extension enabled

**Rationale**:
- SQLite is a single-file embedded database; no server process required — satisfies the offline constraint.
- Handles 50,000+ rows with proper indexing; B-tree indexes on `url` (for duplicate detection) and
  `created_at` (for sorted list) keep add/list within 500 ms at 10 k rows.
- FTS5 virtual table (`bookmarks_fts`) enables ranked full-text search over title, URL, and tags in a
  single SQL query — satisfies SC-003 (search < 1 s at 10 k bookmarks).
- ACID guarantees prevent data corruption on unexpected shutdown.
- `Microsoft.Data.Sqlite` is the canonical .NET wrapper; lightweight, no ORM overhead.

**Alternatives considered**:
- **JSON flat file** (`bookmarks.json`): Simple for small collections, but full table scans for search
  degrade to O(n); no ACID; concurrent write safety is manual. Rejected beyond ~500 bookmarks.
- **LiteDB**: Embedded NoSQL document store; supports indexes and LINQ queries, but FTS is not a native
  feature and dependency is larger. Rejected in favour of SQLite's proven FTS5.
- **Entity Framework Core + SQLite**: Provides LINQ ORM, but adds significant startup overhead and makes
  raw FTS5 queries awkward. Rejected; repository pattern with raw `Dapper`-style SQL is lighter and
  testable.

**Schema highlights** (see data-model.md for full definition):
- `bookmarks` table: id, url (UNIQUE), title, created_at, updated_at
- `tags` table: id, name (UNIQUE)
- `bookmark_tags` join table: bookmark_id, tag_id (composite PK)
- `bookmarks_fts` FTS5 virtual table: mirrors title + url + concatenated tags for full-text search

---

## 3. Search Strategy

**Decision**: SQLite FTS5 `MATCH` query with a pre-built virtual table

**Rationale**:
- FTS5 tokenises and indexes content at write time; reads are O(log n) rather than O(n).
- Supports prefix queries (`keyword*`) automatically.
- The virtual table aggregates title, URL, and tags into a single searchable corpus, so a single
  `WHERE fts MATCH ?` clause satisfies FR-007 without multiple self-joins.
- Trivially keeps the search query result within the 1 s target at 10 k rows (benchmarked reference:
  FTS5 on 100 k rows typically returns in < 50 ms on commodity hardware).

**Alternatives considered**:
- **LIKE query across all columns**: Simple to implement, but `LIKE '%keyword%'` cannot use an index and
  degrades linearly. Rejected for performance reasons.
- **In-memory Lunr/MiniSearch (external library)**: Would require loading all bookmarks into memory;
  not viable at 50 k+ rows. Rejected.

---

## 4. Serialisation Format for Export / Import

**Decision**: JSON via `System.Text.Json` (built-in, zero extra dependency)

**Rationale**:
- Universally understood; importable into other tools; schema is self-documenting.
- `System.Text.Json` ships with .NET 10 — no additional NuGet package required.
- Supports `--json` output flag across list/search/export with a consistent envelope schema.

**Alternatives considered**:
- **CSV**: Does not handle multi-valued tag lists naturally without escaping; harder for users to edit
  by hand safely. Rejected.
- **Newtonsoft.Json**: More features but an unnecessary dependency when `System.Text.Json` is available
  and meets all requirements. Rejected.

---

## 5. Duplicate Detection Key

**Decision**: Full URL string (case-sensitive, including scheme, path, and query string)

**Rationale**:
- Matches user expectation: `https://example.com/page?a=1` and `https://example.com/page?a=2` are
  different resources.
- Implemented as a `UNIQUE` constraint on the `url` column in SQLite — enforced at the database level
  rather than the application layer, preventing races.

**Edge**: URLs that differ only by fragment (`#section`) are considered distinct; fragment is part of
the stored URL string.

---

## 6. Data Store Location

**Decision**: User-local application data directory resolved by `Environment.SpecialFolder.ApplicationData`

**Rationale**:
- Follows OS conventions: `%APPDATA%\BookmarkOrganizer\bookmarks.db` on Windows;
  `~/.config/BookmarkOrganizer/bookmarks.db` on Linux/macOS.
- No elevated permissions required; survives application updates.
- Can be overridden by a `--db` global option for testing and alternate profiles.

**Alternatives considered**:
- **Current working directory**: Not predictable from scripts; breaks when invoked from different paths. Rejected.
- **Hard-coded path**: Violates cross-platform requirement. Rejected.

---

## 7. Dependency Injection

**Decision**: `Microsoft.Extensions.DependencyInjection` (built-in with .NET 10)

**Rationale**:
- Zero-cost built-in container; integrates with `System.CommandLine`'s `IServiceProvider` binding.
- Enables clean construction of `BookmarkService`, `SearchService`, `ImportExportService`, and
  `SqliteBookmarkRepository` with interface segregation — makes unit testing with `Moq` straightforward.

---

## 8. Tag Naming Rules

**Decision**: Tags MUST match the pattern `^[a-z0-9][a-z0-9-]{0,49}$` (lowercase alphanumeric and hyphens, 1–50 characters)

**Rationale**:
- Prevents shell-escaping issues with spaces or special characters.
- Consistent with common tagging conventions (npm, Docker, etc.).
- Normalised to lowercase at write time so `Dev` and `dev` are the same tag.
- Limit of 50 characters prevents pathological tag names.

---

## 9. Testing Libraries

**Decision**: xUnit + FluentAssertions + Moq + BenchmarkDotNet + Coverlet

| Library | Role |
|---------|------|
| `xUnit` | Test runner and test discovery |
| `FluentAssertions` | Readable assertion DSL aligned with AAA pattern |
| `Moq` | Mocking `IBookmarkRepository` in unit tests |
| `BenchmarkDotNet` | Performance benchmarks for add/list/search |
| `Coverlet` | Code coverage collection integrated with `dotnet test` |

**Rationale**: This stack is the standard .NET testing combination; all four packages are actively
maintained and widely adopted. No exotic dependencies.

---

## All NEEDS CLARIFICATION Items — Resolved

| Item | Resolution |
|------|------------|
| CLI framework | `System.CommandLine` |
| Storage | SQLite / `Microsoft.Data.Sqlite` with FTS5 |
| Search | FTS5 virtual table |
| Export format | JSON via `System.Text.Json` |
| Duplicate key | Full URL string (case-sensitive) |
| Data location | `Environment.SpecialFolder.ApplicationData` / `BookmarkOrganizer/` |
| DI container | `Microsoft.Extensions.DependencyInjection` |
| Tag naming | `^[a-z0-9][a-z0-9-]{0,49}$` |
| Test stack | xUnit + FluentAssertions + Moq + BenchmarkDotNet + Coverlet |
