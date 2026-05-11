# Contract: bookmark delete

**Command**: `bookmark delete <id> [options]`  
**User Story**: P4 — Update and Delete Bookmarks  
**Spec FR**: FR-009, FR-013, FR-015

---

## Synopsis

```
bookmark delete <id> [--force]
```

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `<id>` | Yes | Numeric ID of the bookmark to delete. |

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--force` | `-f` | false | Skip confirmation prompt and delete immediately. |

## Behaviour

1. Validate `<id>` exists; error if not found.
2. Unless `--force` is specified, display the bookmark details and prompt:
   ```
   Delete bookmark 42 "Example Site" (https://example.com)? [y/N]:
   ```
   - If the user does not enter `y` or `Y`, abort with exit code **0** and print "Deletion cancelled.".
3. Delete the bookmark row (cascade deletes `bookmark_tags` rows via FK constraint).
4. Remove from FTS5 index.
5. Print confirmation.

## Output — confirmation prompt (no `--force`)

```
Delete bookmark 42 "Example Site" (https://example.com)? [y/N]: y
Bookmark 42 deleted.
```

## Output — cancelled

```
Delete bookmark 42 "Example Site" (https://example.com)? [y/N]: n
Deletion cancelled.
```

## Output — `--force`

```
Bookmark 42 deleted.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Deleted, or deletion cancelled by user |
| 1 | Invalid argument (non-numeric ID) |
| 4 | Bookmark ID not found |
