# Contract: bookmark search

**Command**: `bookmark search <keyword> [options]`  
**User Story**: P3 — Search Bookmarks by Keyword  
**Spec FR**: FR-007, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark search <keyword> [--limit <n>] [--json]
```

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<keyword>` | Yes | Search term. Case-insensitive. Matched against title, URL, and all tag names. Minimum 1 character. |

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--limit <n>` | `-n` | 50 | Maximum number of results. Use `0` for all. |
| `--json` | | false | Output as JSON array. |

## Behaviour

1. Trim and validate `<keyword>` — must be at least 1 non-whitespace character.
2. Execute FTS5 `MATCH` query against `bookmarks_fts` (title, url, tags columns).
3. Match is case-insensitive; partial word matches are supported (prefix matching: `git` matches `github`).
4. Apply `--limit`.
5. Results are returned ordered by FTS5 relevance rank (best match first).
6. Each bookmark appears at most once in the results.

## Output — human-readable (success)

```
2 result(s) for "github".

 ID  Title        URL                   Tags  Added
───  ───────────  ────────────────────  ────  ────────────
  7  GitHub       https://github.com    dev   2026-04-30
 12  GitHub Docs  https://docs.github.com  dev   2026-04-15
```

## Output — JSON (`--json`)

```json
[
  {
    "id": 7,
    "url": "https://github.com",
    "title": "GitHub",
    "tags": ["dev"],
    "created_at": "2026-04-30T08:00:00+00:00",
    "updated_at": "2026-04-30T08:00:00+00:00"
  }
]
```

## Output — no results

```
No bookmarks match "noresult".
Tip: Try a shorter keyword or check your spelling.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success (including empty result) |
| 1 | Invalid argument (blank keyword) |
