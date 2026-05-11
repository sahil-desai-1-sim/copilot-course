# Contract: bookmark update

**Command**: `bookmark update <id> [options]`  
**User Story**: P4 — Update and Delete Bookmarks  
**Spec FR**: FR-008, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark update <id> [--title <title>] [--set-tags <tag>]... [--json]
```

At least one of `--title` or `--set-tags` MUST be provided.

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<id>` | Yes | Numeric ID of the bookmark to update. |

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--title <title>` | `-t` | (no change) | New title. Trimmed; blank value resets title to URL. |
| `--set-tags <tag>` | `-g` | (no change) | Replaces the full tag set with the provided tags. Repeatable. Pass `--set-tags ""` to clear all tags. |
| `--json` | | false | Output updated bookmark as JSON. |

## Behaviour

1. Validate `<id>` exists; error if not found.
2. Validate new title (if provided): trim; default to URL if blank.
3. Validate each tag in `--set-tags` against the tag naming rule.
4. Apply changes in a single transaction: update `title` / `updated_at` on the bookmark; replace tag
   associations in `bookmark_tags`; update FTS5 index.
5. Print the updated bookmark.

## Output — human-readable (success)

```
Bookmark 42 updated.
  ID   : 42
  URL  : https://example.com
  Title: Updated Title
  Tags : dev, reading
  Updated: 2026-05-11 11:00 UTC
```

## Output — JSON (`--json`)

```json
{
  "id": 42,
  "url": "https://example.com",
  "title": "Updated Title",
  "tags": ["dev", "reading"],
  "created_at": "2026-05-11T10:00:00+00:00",
  "updated_at": "2026-05-11T11:00:00+00:00"
}
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Update successful |
| 1 | Invalid argument (bad tag name, no options provided) |
| 4 | Bookmark ID not found |
