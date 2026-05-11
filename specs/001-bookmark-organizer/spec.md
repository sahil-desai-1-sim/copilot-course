# Feature Specification: Bookmark Organizer

**Feature Branch**: `001-bookmark-organizer`  
**Created**: 2026-05-11  
**Status**: Draft  
**Input**: User description: "Bookmark Organizer is a CLI application for storing, tagging, searching, and managing bookmarks locally."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add and Retrieve a Bookmark (Priority: P1)

A user wants to save a URL so they can find it again later. They run a single command to add a bookmark
with a title and optional tags. Immediately afterward they can list their saved bookmarks and see the new
entry with its assigned ID, title, URL, tags, and the date it was saved.

**Why this priority**: Storing and retrieving a bookmark is the irreducible core of the application. Every
other feature builds on top of this. A working add + list flow is a shippable MVP on its own.

**Independent Test**: Run `bookmark add <url> --title <title>`, then run `bookmark list`. Verify the new
bookmark appears with the correct URL, title, and creation date. No other feature is required.

**Acceptance Scenarios**:

1. **Given** no bookmarks exist, **When** the user runs `bookmark add https://example.com --title "Example Site"`, **Then** the system stores the bookmark and prints a confirmation with the assigned ID.
2. **Given** one or more bookmarks exist, **When** the user runs `bookmark list`, **Then** all bookmarks are displayed in a readable table showing ID, title, URL, tags, and date added.
3. **Given** a URL that was already saved, **When** the user attempts to add the same URL again, **Then** the system warns the user of the duplicate, shows the existing bookmark, and does not create a second entry.
4. **Given** the user provides a malformed URL (e.g., missing scheme), **When** they run `bookmark add`, **Then** the system rejects the input with an actionable error message explaining the expected format.

---

### User Story 2 - Tag and Filter Bookmarks (Priority: P2)

A user organises bookmarks by applying descriptive tags (e.g., `dev`, `reading`, `tools`). They can attach
tags at creation time or add/remove tags from an existing bookmark. They can then list only the bookmarks
that match one or more specified tags.

**Why this priority**: Tags are the primary organisational mechanism. Without them, all bookmarks form a
single undifferentiated list that becomes hard to navigate at scale.

**Independent Test**: Add two bookmarks with different tags; run `bookmark list --tag <tag>` and verify
only the matching bookmark appears. Add a tag to the other bookmark and verify the filter now returns both.

**Acceptance Scenarios**:

1. **Given** the user runs `bookmark add <url> --title <title> --tag dev --tag tools`, **When** they run `bookmark list --tag dev`, **Then** only bookmarks tagged `dev` are shown.
2. **Given** an existing bookmark with no tags, **When** the user runs `bookmark tag add <id> reading`, **Then** the tag `reading` is associated with that bookmark and confirmed in subsequent list output.
3. **Given** a bookmark tagged `tools`, **When** the user runs `bookmark tag remove <id> tools`, **Then** the tag is removed and the bookmark no longer appears under `bookmark list --tag tools`.
4. **Given** the user filters by a tag that no bookmark has, **When** they run `bookmark list --tag <unknown-tag>`, **Then** an empty list is displayed with a message stating no bookmarks match.

---

### User Story 3 - Search Bookmarks by Keyword (Priority: P3)

A user wants to find a saved bookmark but can only remember a word from the title, URL, or a tag. They run
a search command and receive a ranked list of bookmarks whose title, URL, or tags contain the keyword.

**Why this priority**: Search becomes essential as the bookmark collection grows. It complements tag
filtering by supporting free-text recall when the user does not remember exact tags.

**Independent Test**: Save three bookmarks with distinct titles and tags. Run `bookmark search <keyword>`
where the keyword appears in only one bookmark's title. Verify only that one bookmark is returned.

**Acceptance Scenarios**:

1. **Given** bookmarks with varying titles and URLs, **When** the user runs `bookmark search "github"`, **Then** all bookmarks whose title, URL, or tags contain "github" (case-insensitive) are listed.
2. **Given** a search term that matches nothing, **When** the user runs `bookmark search <term>`, **Then** an empty result set is shown with a helpful message.
3. **Given** a search term matching entries in multiple fields (title, URL, and tag), **When** the user searches, **Then** each matching bookmark appears exactly once in the results.

---

### User Story 4 - Update and Delete Bookmarks (Priority: P4)

A user needs to correct a bookmark's title, update its tags, or remove a bookmark they no longer need.
They can update any mutable field of a bookmark by its ID, or delete it entirely.

**Why this priority**: Without edit and delete, stale or incorrect bookmarks accumulate and degrade the
quality of the store over time.

**Independent Test**: Add a bookmark, update its title with `bookmark update <id> --title "New Title"`,
then run `bookmark list` to confirm the change. Delete it with `bookmark delete <id>` and confirm it no
longer appears.

**Acceptance Scenarios**:

1. **Given** a bookmark with ID 5, **When** the user runs `bookmark update 5 --title "Better Title"`, **Then** the title is updated and the updated bookmark is displayed.
2. **Given** a bookmark with ID 5, **When** the user runs `bookmark delete 5`, **Then** the bookmark is permanently removed and no longer appears in list or search results.
3. **Given** the user attempts to update or delete a non-existent ID, **When** they run the command, **Then** the system displays an actionable error message stating the ID was not found.
4. **Given** the user runs `bookmark delete <id>` without a `--force` flag, **When** the command is executed, **Then** the system asks for confirmation before deleting; if the user declines, no change occurs.

---

### User Story 5 - Export and Import Bookmarks (Priority: P5)

A user wants to back up their bookmarks or move them to another machine. They export all bookmarks to a
portable file and can later import that file to restore or merge the bookmarks.

**Why this priority**: Portability and backup are important for long-term usability but are not required to
deliver value immediately. Deferred until core functionality is solid.

**Independent Test**: Export bookmarks with `bookmark export --output bookmarks.json`, delete all
bookmarks, then import with `bookmark import bookmarks.json`. Verify all original bookmarks are restored.

**Acceptance Scenarios**:

1. **Given** a collection of bookmarks, **When** the user runs `bookmark export --output <path>`, **Then** a file is created at the specified path containing all bookmarks in a documented, portable format.
2. **Given** an exported file, **When** the user runs `bookmark import <path>`, **Then** all bookmarks from the file are added to the local store; duplicate URLs are skipped with a warning.
3. **Given** a corrupt or unreadable file, **When** the user runs `bookmark import <path>`, **Then** the system reports an actionable error and leaves the existing bookmark store unchanged.

---

### Edge Cases

- What happens when the bookmark store file is missing or corrupted on startup? The system MUST report the problem clearly and not silently discard data.
- What happens when a tag name contains special characters or spaces? The system MUST sanitise or reject invalid tag names with a clear explanation of allowed characters.
- What happens when the user adds a URL with query parameters that differs only in the query string from an existing bookmark? The full URL (including query string) is the deduplication key; these are considered distinct bookmarks.
- What happens when the export destination path does not exist? The system MUST report that the directory cannot be found and suggest creating it.
- What happens when the collection reaches a very large size (> 10,000 bookmarks)? All standard operations MUST still complete within the documented performance targets.

## Requirements *(mandatory)*

<!--
  Constitution reminder (all four principles apply):
  - Code Quality (I): public API must be documented; complexity ≤ 10 per method.
  - Testing Standards (II): ≥ 80 % unit coverage; integration tests for every contract.
  - UX Consistency (III): CLI follows <verb> <noun> [options]; errors are actionable.
  - Performance (IV): standard CLI ops ≤ 500 ms; benchmarks required for hot paths.
-->

### Functional Requirements

- **FR-001**: Users MUST be able to add a bookmark by providing a URL; a title and zero or more tags are optional at add time.
- **FR-002**: The system MUST validate that a provided URL is well-formed (has a valid scheme and host) before storing it and MUST reject malformed URLs with an actionable error message.
- **FR-003**: The system MUST detect duplicate URLs and MUST NOT create a second bookmark for a URL that already exists; instead it MUST display the existing bookmark and a warning.
- **FR-004**: Users MUST be able to list all bookmarks, with each entry showing its ID, title, URL, tags, and date added.
- **FR-005**: Users MUST be able to filter the bookmark list by one or more tags; results MUST include only bookmarks that carry all specified tags.
- **FR-006**: Users MUST be able to add or remove individual tags from an existing bookmark by its ID.
- **FR-007**: Users MUST be able to search bookmarks by a keyword; the search MUST match against title, URL, and tags in a case-insensitive manner.
- **FR-008**: Users MUST be able to update the title and/or tags of an existing bookmark by its ID.
- **FR-009**: Users MUST be able to delete a bookmark by its ID; the system MUST prompt for confirmation before deletion unless a `--force` flag is provided.
- **FR-010**: All bookmarks MUST be persisted locally so they survive application restarts; no network access is required for any standard operation.
- **FR-011**: Users MUST be able to export all bookmarks to a portable file at a specified path.
- **FR-012**: Users MUST be able to import bookmarks from a previously exported file; duplicate URLs encountered during import MUST be skipped with a per-duplicate warning.
- **FR-013**: All CLI commands MUST follow the `<verb> <noun> [options]` structure (Constitution Principle III).
- **FR-014**: The system MUST output results in human-readable format by default; a `--json` flag MUST be supported on list, search, and export commands for machine-readable output.
- **FR-015**: All error messages MUST state what went wrong, why, and what the user can do to resolve the issue (Constitution Principle III).

### Key Entities

- **Bookmark**: Represents a saved web resource. Key attributes: unique ID (system-assigned), URL (required, unique), title (optional, defaults to URL if omitted), tags (list, may be empty), date added, date last modified.
- **Tag**: A short label applied to one or more bookmarks. Attributes: name (alphanumeric and hyphens only), set of associated bookmark IDs.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can save a new bookmark and retrieve it in under 30 seconds on first use with no prior training.
- **SC-002**: All standard operations (add, list, tag, search, delete, update) complete in under 500 ms for collections of up to 10,000 bookmarks.
- **SC-003**: Keyword search returns results in under 1 second for collections of up to 10,000 bookmarks.
- **SC-004**: Bookmarks persist correctly across application restarts with zero data loss under normal operating conditions.
- **SC-005**: 95 % of common tasks (add, list, search, delete) can be completed by a new user using only the built-in help text, without consulting external documentation.
- **SC-006**: Export and subsequent import reproduces the complete bookmark collection with no missing or altered entries.

## Assumptions

- The application targets developers and technically proficient users who are comfortable with CLI tools; a graphical interface is out of scope.
- All bookmark data is stored locally on the user's machine; cloud sync, sharing, and multi-user access are out of scope for v1.
- No browser extension or direct browser integration is required; users add bookmarks manually via the CLI.
- A single bookmark store per user (no workspaces, profiles, or per-project stores) is assumed for v1.
- The underlying storage format (file-based, embedded database, etc.) is an implementation detail and is not prescribed by this specification.
- The application runs on macOS, Linux, and Windows; platform-specific behaviour differences are an implementation concern.
- Internet access is NOT required at runtime; URL validation is format-based only (no HTTP reachability check).
