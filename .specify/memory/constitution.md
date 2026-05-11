<!--
Sync Impact Report
==================
Version change: (none) → 1.0.0
Added sections: Core Principles (I–IV), Development Workflow, Quality Gates, Governance
Removed sections: N/A (first population from template)
Modified principles: N/A (initial ratification)
Templates requiring updates:
  - .specify/templates/plan-template.md ✅ Constitution Check gates align with I–IV
  - .specify/templates/spec-template.md ✅ Requirements section references testing standard
  - .specify/templates/tasks-template.md ✅ Test task guidance consistent with Principle II
Follow-up TODOs: None — all placeholders resolved.
-->

# Copilot Course Constitution

## Core Principles

### I. Code Quality (NON-NEGOTIABLE)

All code MUST be clean, readable, and maintainable. Specifically:

- Methods and functions MUST have a single, clearly defined responsibility (Single Responsibility Principle).
- Cyclomatic complexity per method MUST not exceed 10; refactor any method that exceeds this threshold.
- Magic numbers and hardcoded strings MUST be replaced with named constants or configuration values.
- Dead code, unused imports, and unreachable branches MUST be removed before merging.
- All public APIs MUST be self-documenting through clear naming; XML doc comments are REQUIRED for all public
  members in C# projects.
- Code reviews MUST verify compliance with this principle before any PR is approved.

**Rationale**: Readable, maintainable code reduces defect rates, shortens onboarding time, and lowers the
long-term cost of change. No feature velocity justifies accruing preventable complexity debt.

### II. Testing Standards (NON-NEGOTIABLE)

Automated testing is a first-class engineering activity, not an afterthought.

- Unit test coverage MUST be ≥ 80 % for all business logic and utility code; coverage reports MUST be
  generated and reviewed on every build.
- Tests MUST follow the Arrange-Act-Assert (AAA) pattern with one logical assertion per test case.
- Integration tests MUST cover every externally visible contract (CLI commands, API endpoints, shared schemas).
- Tests MUST be deterministic: flaky tests MUST be quarantined and fixed within one sprint.
- Test names MUST clearly state the scenario and expected outcome (e.g.,
  `Add_GivenNegativeInput_ThrowsArgumentException`).
- No production code MUST be merged without corresponding passing tests.

**Rationale**: A reliable test suite is the safety net that makes continuous delivery possible. Tests that
do not run or do not assert meaningful behaviour provide false confidence and MUST be treated as defects.

### III. User Experience Consistency

Every user-facing interaction MUST conform to a consistent, predictable design language.

- CLI applications MUST use a uniform command structure: `<verb> <noun> [options]` (e.g., `app run task --id 1`).
- Error messages MUST be actionable: state what went wrong, why, and how to fix it — never expose raw
  stack traces to end users.
- Output formatting MUST be consistent: human-readable text by default, with `--json` / `--format` flags
  where machine consumption is expected.
- Help text MUST be provided for every command and flag, and MUST be accurate and up to date.
- Breaking changes to user-visible behaviour (command names, output structure) MUST be deprecated with a
  migration path before removal.

**Rationale**: Consistency reduces the cognitive load on users and builds trust. Unpredictable interfaces
erode confidence and generate unnecessary support burden.

### IV. Performance Requirements

Performance is a feature and MUST be addressed as part of design, not as a post-launch concern.

- Baseline performance benchmarks MUST be established before any feature ships; regressions MUST block
  release.
- CLI commands MUST complete within 500 ms for standard operations on representative hardware unless
  explicitly documented otherwise.
- Memory allocations in hot paths MUST be minimised; any allocation exceeding 1 MB per operation in a
  non-I/O path MUST be justified and documented.
- Long-running operations (> 1 s) MUST provide progress feedback to the user.
- Performance-sensitive code paths MUST include benchmark tests (e.g., BenchmarkDotNet for .NET projects).

**Rationale**: Poor performance is a usability defect. Users abandon slow tools. Establishing measurable
thresholds early prevents normalising degradation over time.

## Development Workflow

The following practices are REQUIRED to uphold the Core Principles during day-to-day work:

- **Branch strategy**: All feature work MUST be done on a dedicated feature branch following the naming
  convention `###-short-description` (e.g., `001-add-login`).
- **Commit discipline**: Commits MUST be atomic and follow Conventional Commits format
  (`feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`).
- **Code review**: Every PR MUST have at least one approving review. Reviewers MUST verify compliance with
  all four Core Principles before approving.
- **Definition of Done**: A task is complete only when: code is written, tests pass, coverage threshold is
  met, documentation is updated, and the PR is merged to the default branch.
- **Linting & formatting**: Automated linters and formatters (e.g., `dotnet format`, EditorConfig) MUST
  run in CI and MUST pass before merge.

## Quality Gates

The following automated gates MUST be enforced in CI on every pull request:

| Gate | Threshold | Blocks Merge |
|------|-----------|--------------|
| Unit test pass rate | 100 % | Yes |
| Code coverage (business logic) | ≥ 80 % | Yes |
| Static analysis / linter | Zero errors | Yes |
| Build (all targets) | Success | Yes |
| Performance benchmarks | No regression vs. baseline | Yes |

Manual gates (required before release):

- UX review confirming output format and error message consistency (Principle III).
- Changelog entry describing user-visible changes.

## Governance

This constitution supersedes all other team norms and informal agreements. Where conflict exists, this
document takes precedence.

**Amendment procedure**:
1. Propose the amendment in a PR that modifies this file.
2. Include rationale, impact assessment, and a migration plan for any breaking change.
3. Obtain approval from at least two project maintainers.
4. Update `CONSTITUTION_VERSION` according to semantic versioning (see below) and set `LAST_AMENDED_DATE`
   to the merge date.

**Versioning policy**:
- MAJOR bump: A Core Principle is removed, fundamentally redefined, or its non-negotiable status is changed.
- MINOR bump: A new principle or section is added, or existing guidance is materially expanded.
- PATCH bump: Clarifications, wording improvements, typo fixes, non-semantic refinements.

**Compliance review**: Compliance with this constitution MUST be verified in every code review. Violations
found after merge MUST be logged as defects and resolved within the current sprint.

**Version**: 1.0.0 | **Ratified**: 2026-05-11 | **Last Amended**: 2026-05-11
