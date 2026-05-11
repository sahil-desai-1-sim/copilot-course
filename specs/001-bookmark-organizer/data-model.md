# Data Model: Bookmark Organizer

**Phase 1 output** | **Date**: 2026-05-11 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

---

## Entities

### Bookmark

Represents a single saved web resource.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `long` | PK, auto-increment, NOT NULL | System-assigned; never user-visible as a primary identifier in messages but used for update/delete |
| `Url` | `string` | NOT NULL, UNIQUE, max 2048 chars | Full URL including scheme, path, and query string; case-sensitive deduplication |
| `Title` | `string` | NOT NULL, max 255 chars | Defaults to the URL value if not provided by the user |
| `CreatedAt` | `DateTimeOffset` | NOT NULL | Set at creation; never updated |
| `UpdatedAt` | `DateTimeOffset` | NOT NULL | Set at creation; updated whenever Title or tags change |
| `Tags` | `IReadOnlyList<Tag>` | Navigation, may be empty | Resolved via `bookmark_tags` join table; not stored directly in this row |

**Validation rules**:
- `Url` MUST have a non-empty scheme (`http`, `https`, `ftp`, etc.) and a non-empty host; validated with
  `Uri.TryCreate(url, UriKind.Absolute, out _)` and `uri.Host.Length > 0`.
- `Title` is trimmed of leading/trailing whitespace; if blank after trim it defaults to `Url`.
- `Url` is stored as-is (case-sensitive); no normalisation is applied beyond trimming.

**State transitions**:

```
[none] --add--> Created
Created --update title/tags--> Updated
Created/Updated --delete--> Deleted (removed from store)
```

---

### Tag

Represents a label that can be applied to zero or more bookmarks.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `long` | PK, auto-increment, NOT NULL | System-assigned |
| `Name` | `string` | NOT NULL, UNIQUE, max 50 chars | Normalised to lowercase at write time |

**Validation rules**:
- `Name` MUST match `^[a-z0-9][a-z0-9-]{0,49}$` (after lowercase normalisation).
- `Name` is normalised to lowercase before storage and before any lookup.
- Tags are created lazily: adding a tag that does not yet exist creates it automatically.
- Tags with no associated bookmarks are NOT automatically deleted (orphan tags are permitted).

---

### BookmarkTag (join)

Represents the many-to-many relationship between bookmarks and tags.

| Field | Type | Constraints |
|-------|------|-------------|
| `BookmarkId` | `long` | FK → Bookmark.Id, NOT NULL |
| `TagId` | `long` | FK → Tag.Id, NOT NULL |

Composite primary key: `(BookmarkId, TagId)`.

---

## Database Schema (SQLite DDL)

```sql
CREATE TABLE IF NOT EXISTS bookmarks (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    url        TEXT    NOT NULL UNIQUE,
    title      TEXT    NOT NULL,
    created_at TEXT    NOT NULL,   -- ISO-8601 with UTC offset
    updated_at TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS tags (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS bookmark_tags (
    bookmark_id INTEGER NOT NULL REFERENCES bookmarks(id) ON DELETE CASCADE,
    tag_id      INTEGER NOT NULL REFERENCES tags(id)      ON DELETE CASCADE,
    PRIMARY KEY (bookmark_id, tag_id)
);

-- Full-text search virtual table (FTS5)
-- Content is denormalised: title + url + space-separated tag names
CREATE VIRTUAL TABLE IF NOT EXISTS bookmarks_fts USING fts5(
    title,
    url,
    tags,           -- space-separated tag names string
    content='',     -- unindexed; application is responsible for keeping in sync
    tokenize='unicode61'
);

-- Indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_bookmarks_created_at ON bookmarks(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_bookmark_tags_tag_id  ON bookmark_tags(tag_id);
```

**FTS5 sync strategy**: The application updates `bookmarks_fts` explicitly in the same SQLite transaction
as any `INSERT`, `UPDATE`, or `DELETE` on `bookmarks` or `bookmark_tags`. This keeps the FTS index
consistent without relying on triggers (which are not available in `Microsoft.Data.Sqlite` by default).

---

## Relationships Diagram

```
┌──────────────┐        ┌───────────────┐        ┌──────────┐
│   Bookmark   │ 0..*   │ bookmark_tags │   0..* │   Tag    │
│──────────────│◄───────│───────────────│────────►│──────────│
│ id           │        │ bookmark_id   │        │ id       │
│ url          │        │ tag_id        │        │ name     │
│ title        │        └───────────────┘        └──────────┘
│ created_at   │
│ updated_at   │
└──────────────┘
           │
           │ (FTS5 virtual table — denormalised read projection)
           ▼
┌────────────────────┐
│   bookmarks_fts    │
│────────────────────│
│ title              │
│ url                │
│ tags (space-sep)   │
└────────────────────┘
```

---

## Export / Import JSON Schema

The portable exchange format used by `bookmark export` and `bookmark import`:

```json
{
  "$schema": "https://bookmark-organizer/schema/v1/export.json",
  "exported_at": "2026-05-11T10:00:00+00:00",
  "version": "1",
  "bookmarks": [
    {
      "url": "https://example.com",
      "title": "Example Site",
      "tags": ["dev", "tools"],
      "created_at": "2026-05-01T09:00:00+00:00",
      "updated_at": "2026-05-01T09:00:00+00:00"
    }
  ]
}
```

**Import rules**:
- `url` is required; records missing `url` are skipped with a per-record warning.
- `title` defaults to `url` if absent or blank.
- `tags` defaults to `[]` if absent.
- `created_at` / `updated_at` are preserved from the export if present; otherwise set to import time.
- Duplicate `url` (already in local store) is skipped with a per-record warning; no overwrite.

---

## C# Domain Models (conceptual — no implementation detail)

```csharp
/// <summary>Represents a saved web bookmark.</summary>
public sealed record Bookmark(
    long Id,
    string Url,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Tag> Tags);

/// <summary>Represents a bookmark tag label.</summary>
public sealed record Tag(long Id, string Name);

/// <summary>Input for creating a new bookmark.</summary>
public sealed record AddBookmarkRequest(
    string Url,
    string? Title,
    IReadOnlyList<string> Tags);

/// <summary>Input for updating an existing bookmark.</summary>
public sealed record UpdateBookmarkRequest(
    long Id,
    string? Title,       // null = no change
    IReadOnlyList<string>? Tags); // null = no change
```
