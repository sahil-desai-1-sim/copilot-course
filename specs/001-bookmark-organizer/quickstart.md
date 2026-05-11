# Quickstart: Bookmark Organizer

**Phase 1 output** | **Date**: 2026-05-11 | **Plan**: [plan.md](plan.md)

---

## Prerequisites

| Requirement | Version | Install |
|-------------|---------|---------|
| .NET SDK | 10.0+ | https://dotnet.microsoft.com/download |
| Git | any | https://git-scm.com |

---

## 1. Clone and Build

```bash
git clone https://github.com/sahil-desai-1-sim/copilot-course.git
cd copilot-course

# Build the bookmark organizer project
dotnet build BookmarkOrganizer/BookmarkOrganizer.csproj
```

## 2. Run the Application

```bash
# From the repository root
dotnet run --project BookmarkOrganizer -- --help
```

Expected output:

```
Description:
  Bookmark Organizer — store, tag, search, and manage bookmarks locally.

Usage:
  bookmark [command] [options]

Commands:
  add      Save a new bookmark
  list     List saved bookmarks
  search   Search bookmarks by keyword
  tag      Add or remove tags from a bookmark
  update   Update a bookmark's title or tags
  delete   Delete a bookmark
  export   Export all bookmarks to a file
  import   Import bookmarks from a file
```

## 3. Add Your First Bookmark

```bash
dotnet run --project BookmarkOrganizer -- add https://github.com --title "GitHub" --tag dev
```

```
Bookmark saved.
  ID   : 1
  URL  : https://github.com
  Title: GitHub
  Tags : dev
  Added: 2026-05-11 10:00 UTC
```

## 4. List, Search, and Tag

```bash
# List all bookmarks
dotnet run --project BookmarkOrganizer -- list

# Filter by tag
dotnet run --project BookmarkOrganizer -- list --tag dev

# Search by keyword
dotnet run --project BookmarkOrganizer -- search "github"

# Add a tag to bookmark ID 1
dotnet run --project BookmarkOrganizer -- tag add 1 tools

# Remove a tag from bookmark ID 1
dotnet run --project BookmarkOrganizer -- tag remove 1 tools
```

## 5. Update and Delete

```bash
# Update title and replace all tags
dotnet run --project BookmarkOrganizer -- update 1 --title "GitHub Home" --set-tags dev --set-tags open-source

# Delete with confirmation
dotnet run --project BookmarkOrganizer -- delete 1

# Delete without confirmation
dotnet run --project BookmarkOrganizer -- delete 1 --force
```

## 6. Export and Import

```bash
# Export to a file
dotnet run --project BookmarkOrganizer -- export --output ~/bookmarks-backup.json

# Import from a file (preview first)
dotnet run --project BookmarkOrganizer -- import ~/bookmarks-backup.json --dry-run

# Import for real
dotnet run --project BookmarkOrganizer -- import ~/bookmarks-backup.json
```

## 7. Run Tests

```bash
# Run all unit and integration tests
dotnet test BookmarkOrganizer.Tests/BookmarkOrganizer.Tests.csproj

# Run with coverage report
dotnet test BookmarkOrganizer.Tests/BookmarkOrganizer.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"
```

## 8. Run Benchmarks

```bash
# Build in Release and run benchmarks
dotnet run --project BookmarkOrganizer.Tests \
  --configuration Release \
  --filter "BookmarkBenchmarks"
```

## 9. Where is my data stored?

| OS | Path |
|----|------|
| Windows | `%APPDATA%\BookmarkOrganizer\bookmarks.db` |
| macOS / Linux | `~/.config/BookmarkOrganizer/bookmarks.db` |

You can override the path with the global `--db <path>` option:

```bash
dotnet run --project BookmarkOrganizer -- --db /tmp/test.db list
```

## 10. Publish a Self-Contained Executable

```bash
# Windows
dotnet publish BookmarkOrganizer -c Release -r win-x64 --self-contained -o ./publish/win

# macOS (Apple Silicon)
dotnet publish BookmarkOrganizer -c Release -r osx-arm64 --self-contained -o ./publish/mac

# Linux x64
dotnet publish BookmarkOrganizer -c Release -r linux-x64 --self-contained -o ./publish/linux
```

The published executable at `./publish/<platform>/bookmark` (or `bookmark.exe` on Windows) can be moved
anywhere on the PATH and invoked directly as `bookmark <command> ...`.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `Error: database is locked` | Another instance of the app is running. Close it and retry. |
| `Error: File not found` on import | Double-check the file path. Paths with spaces must be quoted. |
| Bookmarks not persisting | Confirm the data directory is writable: check `--db` path permissions. |
| `dotnet: command not found` | Install the .NET 10 SDK from https://dotnet.microsoft.com/download |
