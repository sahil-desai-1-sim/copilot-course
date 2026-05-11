# Contract: bookmark list

**Command**: `bookmark list [options]`  
**User Story**: P1 — Add and Retrieve a Bookmark; P2 — Tag and Filter Bookmarks  
**Spec FR**: FR-004, FR-005, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark list [--tag <tag>]... [--limit <n>] [--sort <field>] [--json]
```

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--tag <tag>` | `-g` | (none) | Filter to bookmarks carrying this tag. Repeatable: all specified tags must be present (AND logic). |
| `--limit <n>` | `-n` | 50 | Maximum number of results to return. Use `0` for all. |
| `--sort <field>` | `-s` | `created_at` | Sort field: `created_at` (default, newest first), `updated_at`, `title`. |
| `--json` | | false | Output as JSON array. |

## Behaviour

1. Query the store, applying tag filters (AND) if provided.
2. Sort by `--sort` (descending by date, ascending alphabetically for `title`).
3. Apply `--limit`.
4. If no bookmarks match, print an empty-state message and exit **0**.
5. Print results and exit **0**.

## Output — human-readable (success)

```
3 bookmark(s) found.

 ID  Title                          URL                            Tags            Added
───  ─────────────────────────────  ─────────────────────────────  ──────────────  ────────────
 42  Example Site                   https://example.com            dev, tools      2026-05-11
  7  GitHub                         https://github.com             dev             2026-04-30
  1  Read Later                     https://longread.example.org   reading         2026-03-15
```

## Output — JSON (`--json`)

```json
[
  {
    "id": 42,
    "url": "https://example.com",
    "title": "Example Site",
    "tags": ["dev", "tools"],
    "created_at": "2026-05-11T10:00:00+00:00",
    "updated_at": "2026-05-11T10:00:00+00:00"
  }
]
```

## Output — empty result

```
No bookmarks found.
Tip: Use 'bookmark add <url>' to save your first bookmark.
```

(With `--tag filter`:)
```
No bookmarks found matching tag(s): reading, tools.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success (including empty result) |
| 1 | Invalid option value |
