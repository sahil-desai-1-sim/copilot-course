# Contract: bookmark export

**Command**: `bookmark export [options]`  
**User Story**: P5 — Export and Import Bookmarks  
**Spec FR**: FR-011, FR-013, FR-014, FR-015

---

## Synopsis

```
bookmark export [--output <path>] [--json]
```

## Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--output <path>` | `-o` | stdout | File path to write the exported JSON. If omitted, output is written to stdout. |
| `--json` | | true (implied) | Export format is always JSON. This flag enables consistent `--json` UX for piping; it has no effect on format selection. |

## Behaviour

1. Read all bookmarks from the store.
2. Serialise to the [export JSON schema](../data-model.md#export--import-json-schema) (version 1).
3. If `--output` is specified:
   - Verify the parent directory exists; error if not (do NOT create directories silently).
   - Write the file. If the file already exists, overwrite it (no prompt — the path was explicitly given).
4. If `--output` is not specified, write JSON to stdout.
5. Print a summary to stderr (so it does not pollute stdout when piping):
   ```
   Exported 42 bookmark(s) to bookmarks.json.
   ```

## Output — stdout (no `--output`)

Full JSON document matching the export schema.

## Output — summary to stderr (with `--output`)

```
Exported 42 bookmark(s) to /home/user/bookmarks.json.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Export successful |
| 1 | Target directory not found |
| 3 | Write error (permissions, disk full, etc.) |
