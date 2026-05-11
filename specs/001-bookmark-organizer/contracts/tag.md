# Contract: bookmark tag

**Command**: `bookmark tag <add|remove> <id> <tag> [options]`  
**User Story**: P2 — Tag and Filter Bookmarks  
**Spec FR**: FR-006, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark tag add    <id> <tag>
bookmark tag remove <id> <tag>
```

## Subcommands

| Subcommand | Description |
|------------|-------------|
| `add` | Add a tag to an existing bookmark. Creates the tag if it does not exist. |
| `remove` | Remove a tag from a bookmark. No-ops silently if the tag is not currently applied. |

## Arguments (both subcommands)

| Argument | Required | Description |
|----------|----------|-------------|
| `<id>` | Yes | Numeric ID of the bookmark to modify. |
| `<tag>` | Yes | Tag name. Must match `^[a-z0-9][a-z0-9-]{0,49}$` (normalised to lowercase). |

## Behaviour — `bookmark tag add`

1. Validate `<id>` exists; error if not found.
2. Validate `<tag>` name.
3. If the tag does not exist in `tags` table, create it.
4. Insert into `bookmark_tags` (silently no-op if already present — idempotent).
5. Update `updated_at` on the bookmark.
6. Update FTS5 index.
7. Print confirmation.

## Behaviour — `bookmark tag remove`

1. Validate `<id>` exists; error if not found.
2. Validate `<tag>` name.
3. Delete from `bookmark_tags` where `(bookmark_id, tag_id)` matches.
4. If the join row did not exist, still exit **0** with a note.
5. Update `updated_at` on the bookmark.
6. Update FTS5 index.

## Output — `add` success

```
Tag 'reading' added to bookmark 42 (Example Site).
```

## Output — `remove` success

```
Tag 'reading' removed from bookmark 42 (Example Site).
```

## Output — tag not present (remove, no-op)

```
Note: Bookmark 42 did not have tag 'reading'. No change made.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success (including no-op on remove) |
| 1 | Invalid argument (bad tag name, non-numeric ID) |
| 4 | Bookmark ID not found |
