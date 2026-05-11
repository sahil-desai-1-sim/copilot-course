# Contract: bookmark import

**Command**: `bookmark import <path> [options]`  
**User Story**: P5 — Export and Import Bookmarks  
**Spec FR**: FR-012, FR-013, FR-015

---

## Synopsis

```
bookmark import <path> [--dry-run]
```

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<path>` | Yes | Path to the JSON file previously produced by `bookmark export`. |

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--dry-run` | | false | Parse and validate the file without writing anything; prints what would be imported. |

## Behaviour

1. Read and parse the file at `<path>`; error if the file does not exist or is not valid JSON.
2. Validate the top-level schema (`version`, `bookmarks` array); error if malformed.
3. For each bookmark record:
   - If `url` is missing or blank, skip with a per-record warning.
   - If `url` already exists in the local store, skip with a per-record warning.
   - Otherwise, import the record (with its tags, title, timestamps).
4. All valid records are inserted in a single transaction — if the transaction fails, no records are written.
5. Print a summary of imported, skipped (duplicate), and skipped (invalid) counts.
6. If `--dry-run`, nothing is written; print the same summary prefixed with "[DRY RUN]".

## Output — success

```
Import complete.
  Imported : 38
  Skipped  : 3 duplicate URL(s)
  Invalid  : 1 missing URL field (record #12)

Run 'bookmark list' to see your bookmarks.
```

## Output — `--dry-run`

```
[DRY RUN] Would import:
  Import   : 38
  Skip     : 3 duplicate URL(s)
  Invalid  : 1 missing URL field (record #12)

No changes were made.
```

## Output — file not found

```
Error: File not found: /home/user/missing.json
Check the path and try again.
```

## Output — malformed JSON

```
Error: The file '/home/user/bookmarks.json' is not valid JSON or does not match the expected export schema.
Ensure the file was produced by 'bookmark export'. No bookmarks were imported.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Import completed (even if some records were skipped) |
| 1 | File not found or unreadable |
| 5 | File is not valid JSON / does not match expected schema |
