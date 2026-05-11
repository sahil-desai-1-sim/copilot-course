---
description: "Task list for Bookmark Organizer implementation"
---

# Tasks: Bookmark Organizer

**Input**: Design documents from `specs/001-bookmark-organizer/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Per Constitution Principle II (Testing Standards), unit tests (≥ 80 % coverage) and integration
tests for every CLI command contract are REQUIRED throughout all phases.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: User story label — [US1] through [US5]

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the two .NET projects, wire them into the solution, and install all dependencies.

- [X] T001 Create `BookmarkOrganizer/BookmarkOrganizer.csproj` (.NET 10 console app, nullable + implicit usings enabled) and add it to `copilot-course.sln`
- [X] T002 Create `BookmarkOrganizer.Tests/BookmarkOrganizer.Tests.csproj` (xUnit test project targeting .NET 10) and add it to `copilot-course.sln`
- [X] T003 Add NuGet packages to `BookmarkOrganizer/BookmarkOrganizer.csproj`: `System.CommandLine`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.DependencyInjection`
- [X] T004 [P] Add NuGet packages to `BookmarkOrganizer.Tests/BookmarkOrganizer.Tests.csproj`: `xunit`, `FluentAssertions`, `Moq`, `BenchmarkDotNet`, `coverlet.collector`, `Microsoft.NET.Test.Sdk`
- [X] T005 [P] Add `.editorconfig` at repository root enforcing C# style rules (`indent_size = 4`, `dotnet_sort_system_directives_first = true`, `csharp_new_line_before_open_brace = all`)

**Checkpoint**: `dotnet build` succeeds on both projects with zero warnings.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain models, repository abstraction, SQLite initialisation, and DI wiring that ALL user
stories depend on. No story work begins until this phase is complete.

**⚠️ CRITICAL**: Every subsequent phase depends on T006–T016 being complete and green.

- [X] T006 Create `BookmarkOrganizer/Constants.cs` — define all string/numeric constants (table names, column names, tag pattern `^[a-z0-9][a-z0-9-]{0,49}$`, default limit 50, max URL length 2048, max title length 255, max tag length 50)
- [X] T007 [P] Create `BookmarkOrganizer/Models/Bookmark.cs` — `sealed record Bookmark(long Id, string Url, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<Tag> Tags)` with XML doc comments
- [X] T008 [P] Create `BookmarkOrganizer/Models/Tag.cs` — `sealed record Tag(long Id, string Name)` with XML doc comments
- [X] T009 [P] Create `BookmarkOrganizer/Models/AddBookmarkRequest.cs` and `UpdateBookmarkRequest.cs` request records with XML doc comments
- [X] T010 Define `BookmarkOrganizer/Infrastructure/IBookmarkRepository.cs` — interface with methods: `AddAsync`, `GetAllAsync`, `GetByIdAsync`, `GetByTagsAsync`, `UpdateAsync`, `DeleteAsync`, `FindByUrlAsync`, `AddTagAsync`, `RemoveTagAsync`, all with XML doc comments
- [X] T011 Create `BookmarkOrganizer/Infrastructure/DatabaseInitializer.cs` — executes `CREATE TABLE IF NOT EXISTS` DDL for `bookmarks`, `tags`, `bookmark_tags`, and `bookmarks_fts` FTS5 virtual table as defined in `data-model.md`
- [X] T012 Create `BookmarkOrganizer/Infrastructure/MigrationRunner.cs` — applies versioned schema migrations stored in an `_migrations` table; idempotent on re-run
- [X] T013 Implement `BookmarkOrganizer/Infrastructure/SqliteBookmarkRepository.cs` — full implementation of `IBookmarkRepository` using `Microsoft.Data.Sqlite`; FTS5 index kept in sync within the same transaction on every write
- [X] T014 [P] Create `BookmarkOrganizer/Infrastructure/DataStorePath.cs` — resolves `Environment.SpecialFolder.ApplicationData/BookmarkOrganizer/bookmarks.db`; overridable via a `--db` global option value passed in at startup
- [X] T015 Wire DI container and root `System.CommandLine` command in `BookmarkOrganizer/Program.cs` — register `IBookmarkRepository` → `SqliteBookmarkRepository`, `DatabaseInitializer`, `MigrationRunner`; run migrations on startup; add `--db` global option
- [X] T016 [P] Write unit tests for `Bookmark` and `Tag` model validation helpers in `BookmarkOrganizer.Tests/Unit/Models/BookmarkTests.cs`
- [X] T017 Write integration tests for `SqliteBookmarkRepository` (all `IBookmarkRepository` methods, FTS5 sync, cascade delete) in `BookmarkOrganizer.Tests/Integration/Infrastructure/SqliteBookmarkRepositoryTests.cs` — use an in-memory SQLite database per test

**Checkpoint**: `dotnet test --filter "Category=Foundational"` is green; all repository operations verified.

---

## Phase 3: User Story 1 — Add and Retrieve a Bookmark (Priority: P1) 🎯 MVP

**Goal**: A user can `bookmark add <url>` and immediately see it with `bookmark list`. Delivers the
irreducible shippable MVP.

**Independent Test**: `dotnet run --project BookmarkOrganizer -- add https://example.com --title "Test"` prints confirmation with an ID; `bookmark list` shows the entry. No other phase required.

### Tests for User Story 1

- [X] T018 [P] [US1] Write unit tests for `BookmarkService.AddAsync` (success, URL validation, duplicate detection) and `BookmarkService.GetAllAsync` in `BookmarkOrganizer.Tests/Unit/Services/BookmarkServiceTests.cs`
- [X] T019 [P] [US1] Write integration tests for `AddCommand` (success, duplicate URL exits 2, malformed URL exits 1, --json output) in `BookmarkOrganizer.Tests/Integration/Commands/AddCommandTests.cs`
- [X] T020 [P] [US1] Write integration tests for `ListCommand` (shows all bookmarks, empty-state message, --json output, --limit, --sort) in `BookmarkOrganizer.Tests/Integration/Commands/ListCommandTests.cs`

### Implementation for User Story 1

- [X] T021 [US1] Define `BookmarkOrganizer/Services/IBookmarkService.cs` — declare `AddAsync`, `GetAllAsync`; include XML doc comments
- [X] T022 [US1] Implement `BookmarkOrganizer/Services/BookmarkService.cs` — `AddAsync` (validates URL with `Uri.TryCreate`, trims title, normalises tags to lowercase, checks duplicate via `FindByUrlAsync`, delegates to repo); `GetAllAsync` (delegates to repo with sort/limit)
- [X] T023 [P] [US1] Implement `BookmarkOrganizer/Commands/AddCommand.cs` — `System.CommandLine` command with `<url>` argument, `--title`, `--tag` (repeatable), `--json` options; human-readable and JSON output per `contracts/add.md`; exit codes 0/1/2
- [X] T024 [P] [US1] Implement `BookmarkOrganizer/Commands/ListCommand.cs` — `System.CommandLine` command with `--limit`, `--sort`, `--json` options; table output and JSON array per `contracts/list.md`; empty-state message
- [X] T025 [US1] Register `IBookmarkService` → `BookmarkService`, `AddCommand`, `ListCommand` in `BookmarkOrganizer/Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US1"` is green; MVP end-to-end add → list flow works.

---

## Phase 4: User Story 2 — Tag and Filter Bookmarks (Priority: P2)

**Goal**: Users can apply tags at add time or via `bookmark tag add/remove`, and filter `bookmark list`
by one or more tags.

**Independent Test**: Add two bookmarks with different tags; `bookmark list --tag <tag>` returns only the matching one; `bookmark tag add <id> <tag>` extends the filter to include the other.

### Tests for User Story 2

- [ ] T026 [P] [US2] Write unit tests for `BookmarkService.AddTagAsync`, `RemoveTagAsync`, and `GetByTagsAsync` in `BookmarkOrganizer.Tests/Unit/Services/BookmarkServiceTests.cs`
- [ ] T027 [P] [US2] Write integration tests for `TagCommand` (add success, remove success, remove no-op, invalid tag name, bookmark not found) in `BookmarkOrganizer.Tests/Integration/Commands/TagCommandTests.cs`
- [ ] T028 [P] [US2] Write integration tests for `ListCommand --tag` filter (single tag, multi-tag AND, no matches) in `BookmarkOrganizer.Tests/Integration/Commands/ListCommandTests.cs`

### Implementation for User Story 2

- [ ] T029 [US2] Extend `IBookmarkService` and `BookmarkService` with `AddTagAsync(long id, string tag)`, `RemoveTagAsync(long id, string tag)`, and `GetByTagsAsync(IReadOnlyList<string> tags, int limit, string sort)` in `BookmarkOrganizer/Services/`
- [ ] T030 [P] [US2] Implement `BookmarkOrganizer/Commands/TagCommand.cs` — `bookmark tag` parent command with `add` and `remove` subcommands per `contracts/tag.md`; validates tag name against pattern in `Constants.cs`; exit codes 0/1/4
- [ ] T031 [P] [US2] Extend `BookmarkOrganizer/Commands/ListCommand.cs` to accept `--tag` (repeatable) and pass the tag list to `BookmarkService.GetByTagsAsync`
- [ ] T032 [US2] Register `TagCommand` and updated `ListCommand` in `BookmarkOrganizer/Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US2"` is green; tag add/remove/filter all work independently.

---

## Phase 5: User Story 3 — Search Bookmarks by Keyword (Priority: P3)

**Goal**: Users can run `bookmark search <keyword>` to find bookmarks by title, URL, or tag via FTS5.

**Independent Test**: Add three distinct bookmarks; `bookmark search <keyword-in-one-title>` returns exactly one result.

### Tests for User Story 3

- [ ] T033 [P] [US3] Write unit tests for `SearchService.SearchAsync` (match in title, URL, tag, no results, deduplication) in `BookmarkOrganizer.Tests/Unit/Services/SearchServiceTests.cs`
- [ ] T034 [P] [US3] Write integration tests for `SearchCommand` (results found, no results, blank keyword exits 1, --json output) in `BookmarkOrganizer.Tests/Integration/Commands/SearchCommandTests.cs`

### Implementation for User Story 3

- [ ] T035 [US3] Define `BookmarkOrganizer/Services/ISearchService.cs` and implement `BookmarkOrganizer/Services/SearchService.cs` — executes FTS5 `MATCH` query on `bookmarks_fts` via `IBookmarkRepository`; results ordered by FTS5 rank; applies `--limit`
- [ ] T036 [P] [US3] Implement `BookmarkOrganizer/Commands/SearchCommand.cs` — `<keyword>` argument, `--limit`, `--json` options; table output and JSON array per `contracts/search.md`; no-results message; exit codes 0/1
- [ ] T037 [US3] Register `ISearchService` → `SearchService` and `SearchCommand` in `BookmarkOrganizer/Program.cs`; extend `IBookmarkRepository` with `SearchAsync(string keyword, int limit)` and implement in `SqliteBookmarkRepository`

**Checkpoint**: `dotnet test --filter "Category=US3"` is green; FTS5 search returns ranked, deduplicated results.

---

## Phase 6: User Story 4 — Update and Delete Bookmarks (Priority: P4)

**Goal**: Users can correct titles/tags with `bookmark update` and remove stale bookmarks with
`bookmark delete` (with confirmation guard).

**Independent Test**: Add a bookmark; `bookmark update <id> --title "New"` changes the title; `bookmark delete <id>` removes it; `bookmark list` confirms absence.

### Tests for User Story 4

- [ ] T038 [P] [US4] Write unit tests for `BookmarkService.UpdateAsync` and `BookmarkService.DeleteAsync` (success, not-found, no options provided) in `BookmarkOrganizer.Tests/Unit/Services/BookmarkServiceTests.cs`
- [ ] T039 [P] [US4] Write integration tests for `UpdateCommand` (title update, tag replacement, not-found exits 4, no options exits 1, --json) in `BookmarkOrganizer.Tests/Integration/Commands/UpdateCommandTests.cs`
- [ ] T040 [P] [US4] Write integration tests for `DeleteCommand` (confirmation accepted, confirmation declined, --force, not-found exits 4) in `BookmarkOrganizer.Tests/Integration/Commands/DeleteCommandTests.cs`

### Implementation for User Story 4

- [ ] T041 [US4] Extend `IBookmarkService` and `BookmarkService` with `UpdateAsync(UpdateBookmarkRequest request)` (updates title and/or full tag set in a single transaction, updates FTS5 index) and `DeleteAsync(long id)` (cascade delete via FK, removes from FTS5 index) in `BookmarkOrganizer/Services/`
- [ ] T042 [P] [US4] Implement `BookmarkOrganizer/Commands/UpdateCommand.cs` — `<id>` argument, `--title`, `--set-tags` (repeatable), `--json`; validates at least one option present; per `contracts/update.md`; exit codes 0/1/4
- [ ] T043 [P] [US4] Implement `BookmarkOrganizer/Commands/DeleteCommand.cs` — `<id>` argument, `--force` flag; confirmation prompt to stdout with `[y/N]`; per `contracts/delete.md`; exit codes 0/1/4
- [ ] T044 [US4] Register `UpdateCommand` and `DeleteCommand` in `BookmarkOrganizer/Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US4"` is green; update and delete (with and without `--force`) work correctly.

---

## Phase 7: User Story 5 — Export and Import Bookmarks (Priority: P5)

**Goal**: Users can back up with `bookmark export` and restore with `bookmark import`, with duplicate
skipping and a `--dry-run` preview mode.

**Independent Test**: Export all bookmarks; force-delete all; import from the export file; verify complete restoration.

### Tests for User Story 5

- [ ] T045 [P] [US5] Write unit tests for `ImportExportService.ExportAsync` and `ImportExportService.ImportAsync` (success, schema validation, duplicate skip, missing-url skip, malformed JSON) in `BookmarkOrganizer.Tests/Unit/Services/ImportExportServiceTests.cs`
- [ ] T046 [P] [US5] Write integration tests for `ExportCommand` (stdout output, --output file, directory-not-found exits 1, --json implied) in `BookmarkOrganizer.Tests/Integration/Commands/ExportCommandTests.cs`
- [ ] T047 [P] [US5] Write integration tests for `ImportCommand` (full round-trip, duplicate skipped with warning, malformed file exits 5, --dry-run shows preview without writing) in `BookmarkOrganizer.Tests/Integration/Commands/ImportCommandTests.cs`

### Implementation for User Story 5

- [ ] T048 [US5] Define `BookmarkOrganizer/Services/IImportExportService.cs` and implement `BookmarkOrganizer/Services/ImportExportService.cs` — `ExportAsync` serialises all bookmarks to export schema v1 JSON via `System.Text.Json`; `ImportAsync` deserialises, validates schema, skips duplicates, inserts valid records in a single transaction; supports `--dry-run`
- [ ] T049 [P] [US5] Implement `BookmarkOrganizer/Commands/ExportCommand.cs` — `--output` option (defaults to stdout), summary line to stderr; per `contracts/export.md`; exit codes 0/1/3
- [ ] T050 [P] [US5] Implement `BookmarkOrganizer/Commands/ImportCommand.cs` — `<path>` argument, `--dry-run` flag; per `contracts/import.md`; exit codes 0/1/5
- [ ] T051 [US5] Register `IImportExportService` → `ImportExportService`, `ExportCommand`, `ImportCommand` in `BookmarkOrganizer/Program.cs`

**Checkpoint**: `dotnet test --filter "Category=US5"` is green; full export → wipe → import round-trip restores all bookmarks.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Performance validation, coverage verification, error message audit, and final quality gates.

- [ ] T052 [P] Implement `BookmarkOrganizer.Tests/Benchmarks/BookmarkBenchmarks.cs` — BenchmarkDotNet benchmarks for `AddAsync`, `GetAllAsync`, and `SearchAsync` each seeded with 10,000 rows; verify p99 stays within SC-002 / SC-003 targets (500 ms / 1 s)
- [ ] T053 [P] Audit all error messages across every `Command` file against Constitution Principle III (what / why / how) and FR-015; update any message that does not include all three elements
- [ ] T054 [P] Run `dotnet test` with Coverlet coverage collection; verify line coverage ≥ 80 % for `BookmarkOrganizer/Services/` and `BookmarkOrganizer/Infrastructure/`; add missing tests for any uncovered branch
- [ ] T055 [P] Add XML doc comments to any public member in `BookmarkOrganizer/` that is missing them (Constitution Principle I audit)
- [ ] T056 [P] Write an end-to-end smoke test in `BookmarkOrganizer.Tests/Integration/` that exercises the full user journey: add → list → tag add → list --tag → search → update → delete --force → export → import --dry-run → import
- [ ] T057 Run `dotnet format BookmarkOrganizer/ BookmarkOrganizer.Tests/` and resolve all formatting warnings; confirm zero `dotnet build` warnings on both projects

**Checkpoint**: All 57 tasks complete; `dotnet test` 100 % pass; coverage ≥ 80 %; `dotnet build` zero warnings; benchmarks within targets.

---

## Dependencies

```
Phase 1 (T001–T005)
  └─► Phase 2 (T006–T017)
        └─► Phase 3 (T018–T025) — US1 MVP; independent after Phase 2
        └─► Phase 4 (T026–T032) — US2; depends on Phase 3 (BookmarkService exists)
        └─► Phase 5 (T033–T037) — US3; depends on Phase 2 only (separate SearchService)
        └─► Phase 6 (T038–T044) — US4; depends on Phase 3 (BookmarkService)
        └─► Phase 7 (T045–T051) — US5; depends on Phase 3 (BookmarkService for import)
              └─► Phase 8 (T052–T057) — Polish; depends on all story phases complete
```

**Cross-story independence after Phase 2**:
- US3 (Search, T033–T037) has NO dependency on US2 and can be implemented in parallel with US2.
- US5 (Export/Import, T045–T051) depends on Phase 3 for `BookmarkService` but is otherwise independent.

---

## Parallel Execution Examples

### Implement US1, US3, and US5 tests simultaneously (after Phase 2 complete)

```
Developer A: T018, T019, T020  (US1 test writing)
Developer B: T033, T034         (US3 test writing)
Developer C: T045, T046, T047   (US5 test writing)
```

### Implement commands in parallel within a story (after service layer complete)

**US1 commands** (after T022 — `BookmarkService` ready):
```
Developer A: T023  (AddCommand.cs)
Developer B: T024  (ListCommand.cs)
```

**US4 commands** (after T041 — `BookmarkService` Update/Delete ready):
```
Developer A: T042  (UpdateCommand.cs)
Developer B: T043  (DeleteCommand.cs)
```

**US5 commands** (after T048 — `ImportExportService` ready):
```
Developer A: T049  (ExportCommand.cs)
Developer B: T050  (ImportCommand.cs)
```

### Polish phase — all tasks are independent

```
T052 (Benchmarks) | T053 (Error message audit) | T054 (Coverage) | T055 (XML docs) | T056 (E2E smoke)
```

---

## Implementation Strategy

**MVP scope (Phase 1 + 2 + 3 only — T001–T025)**:
- Produces a working CLI that can add and list bookmarks.
- Sufficient to demonstrate core value and validate the architecture end-to-end.
- Estimated 25 tasks; all subsequent stories layer on top without rework.

**Incremental delivery order**:
1. T001–T017 — foundation (can be done in one session)
2. T018–T025 — US1 MVP (add + list)
3. T026–T032 — US2 (tagging + filter)
4. T033–T037 — US3 (search) — can run in parallel with US2
5. T038–T044 — US4 (update + delete)
6. T045–T051 — US5 (export + import)
7. T052–T057 — polish

**Total tasks**: 57  
**Parallelisable tasks**: 34 (marked [P])  
**Sequential gates**: Phase 1 → Phase 2 → Phase 3 → (US2, US3, US4, US5 in any order) → Polish
