# Contract: bookmark add

**Command**: `bookmark add <url> [options]`  
**User Story**: P1 — Add and Retrieve a Bookmark  
**Spec FR**: FR-001, FR-002, FR-003, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark add <url> [--title <title>] [--tag <tag>]... [--json]
```

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<url>` | Yes | The URL to bookmark. Must be a well-formed absolute URL with a valid scheme and host. |

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--title <title>` | `-t` | (URL value) | Human-readable title for the bookmark. Defaults to the URL if omitted or blank. |
| `--tag <tag>` | `-g` | (none) | Tag to apply. Repeatable: `--tag dev --tag tools`. Must match `^[a-z0-9][a-z0-9-]{0,49}$`. |
| `--json` | | false | Output result as JSON instead of human-readable text. |

## Behaviour

1. Validate `<url>`: must be an absolute URI with a non-empty host.
2. Normalise: trim whitespace from URL and title; lowercase all tag names.
3. Check for duplicate URL in the store.
   - If duplicate exists: print a warning showing the existing bookmark and exit with code **2** (no write performed).
4. If not a duplicate: persist the bookmark and its tags.
5. Print confirmation (or JSON result) and exit with code **0**.

## Output — human-readable (success)

```
Bookmark saved.
  ID   : 42
  URL  : https://example.com
  Title: Example Site
  Tags : dev, tools
  Added: 2026-05-11 10:00 UTC
```

## Output — JSON (success, `--json`)

```json
{
  "id": 42,
  "url": "https://example.com",
  "title": "Example Site",
  "tags": ["dev", "tools"],
  "created_at": "2026-05-11T10:00:00+00:00",
  "updated_at": "2026-05-11T10:00:00+00:00"
}
```

## Output — duplicate warning (exit 2)

```
Warning: This URL is already saved (ID 7).
  ID   : 7
  URL  : https://example.com
  Title: Example Site
  Tags : dev
No new bookmark was created. Use 'bookmark update 7' to modify it.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Bookmark saved successfully |
| 1 | Invalid argument (bad URL, bad tag name) |
| 2 | Duplicate URL — no write performed |
