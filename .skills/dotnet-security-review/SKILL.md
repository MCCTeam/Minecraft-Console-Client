---
name: dotnet-security-review
description: >-
  Performs a systematic C#/ASP.NET Core security code review on .NET 8 (C# 12)
  and .NET 10 (C# 14) codebases. Covers OWASP Top 10, authentication/authorization
  audit, input validation, cryptography, dependency vulnerabilities, security
  headers, middleware pipeline, and CI/CD security posture.
metadata:
  platform: ".NET 8 and .NET 10 (no .NET 9 projects in scope)"
---

# Security Code Review for C# / ASP.NET Core (.NET 8 + .NET 10)

You are a security auditor performing a thorough, evidence-based code review. Every finding MUST include file path, line number, severity, impact, and a concrete fix.

## Step 0 — Detect the target framework

Before scoring findings, follow `../../references/detect-target-framework.md`. The security guidance below applies to **both** .NET 8 and .NET 10 unless explicitly marked. A few items are .NET 10-only — when reviewing a .NET 8 project, don't recommend them as "fixes":

- **ASP.NET Core Identity passkeys** (`AddPasskeys()`) — .NET 10 only. On .NET 8, recommend external IdP / `Fido2NetLib` or password+TOTP.
- **Minimal-API built-in validation** (`AddValidation()`) — .NET 10 only. On .NET 8, FluentValidation + `IEndpointFilter` is the safe equivalent.
- **First-party `Microsoft.AspNetCore.OpenApi`** — .NET 9+ only. On .NET 8 the project should use `Swashbuckle.AspNetCore`; flag missing OpenAPI security schemes accordingly.
- **`HybridCache`** — .NET 9+ only. On .NET 8 verify `IDistributedCache` configurations (encryption-at-rest, key prefixing, TLS to Redis) directly.
- The C# 14 `field` keyword, `extension(...)` blocks, null-conditional assignment, and partial constructors **do not compile on net8.0** — never propose security fixes that introduce them on a .NET 8 project.

All cryptography, JWT, authorization-policy, header, and middleware guidance applies identically on both targets.

## Target Selection

The user's arguments are in `$ARGUMENTS`.

- If `$ARGUMENTS` contains a file path or directory, review that target.
- If `$ARGUMENTS` is "all", review the entire codebase starting from the solution root.
- If `$ARGUMENTS` is empty, run `git diff --name-only HEAD~5` to find recently changed `.cs` files. If none, ask the user what to review.

When reviewing a directory or "all", use Glob to find `**/*.cs` files, then prioritize:
1. Controllers, filters, middleware (`*Controller.cs`, `*Filter.cs`, `Program.cs`)
2. Auth handlers and delegating handlers (`*Handler.cs`, `*DelegatingHandler.cs`)
3. Service implementations handling external input or secrets
4. Repository and data access code
5. Configuration and DI registration (`*Extensions.cs`, `*Options.cs`)
6. Validators

## Review Process

Execute each phase sequentially. Use the Read tool for files and the Grep tool for pattern searches. NEVER use bash `grep` or `rg` -- always use the Grep tool.

### Phase 1: Automated Pattern Scanning

Read `references/scanning-patterns.md` for the full pattern catalog. Run all Grep searches in parallel across `.cs` files in the target scope. Each pattern targets a specific vulnerability class: injection, deserialization, cryptography, async anti-patterns, data exposure, SSRF, missing controls, ReDoS, log injection, open redirect, cookie security, file upload, claims safety, and thread safety.

### Phase 2: File-by-File Deep Review

Read `references/deep-review-categories.md` for the complete checklist (Categories A through L). For each file in scope (or top ~20 most security-relevant files when reviewing "all"), check all applicable categories:

- **A**: Authentication & Authorization (JWT validation, auth schemes, IDOR)
- **B**: Input Validation
- **C**: Error Handling & Information Leakage
- **D**: Cryptography & Secrets
- **E**: Data Protection & PII
- **F**: Concurrency & State Safety
- **G**: CancellationToken Propagation
- **H**: HTTP Client Security (resilience handlers, DNS refresh)
- **I**: Configuration Security
- **J**: Logging & Monitoring Security
- **K**: Output Encoding & Response Security
- **L**: Supply Chain & Build Security

### Phase 3: Architecture & Project-Specific Checks

Read `references/architecture-checks.md` for checks tailored to common ASP.NET Core project patterns. These cover endpoint authorization verification, anonymous endpoint abuse potential, OTP/MFA security, exception handling coverage, optimistic concurrency, state expiry, blob storage SAS security, message queue security, JSON serialization settings, background task queue safety, rate limiting, security headers, middleware ordering, and NuGet audit configuration.

Read the project's CLAUDE.md or AGENTS.md for project-specific architecture details to inform these checks.

### Phase 4: Dependency Vulnerability Check

Read `references/dependencies-and-headers.md` (Phase 4 section) for dependency scanning patterns. Check `.csproj` files for known-vulnerable versions and NuGet audit configuration.

### Phase 5: Security Headers & Middleware Pipeline

Read `references/dependencies-and-headers.md` (Phase 5 section) for the 14-item headers checklist and middleware ordering verification.

## Output Format

### Security Review Report

**Scope:** [files/directories reviewed]
**Date:** [current date]
**Risk Summary:** [X CRITICAL, Y HIGH, Z MEDIUM, W LOW, V INFO]

#### Findings

For each finding:

**[SEVERITY] [SHORT-TITLE]**
- **Location:** `file/path.cs:LINE`
- **Category:** [OWASP category or security domain]
- **Description:** [What the vulnerability is and why it matters]
- **Impact:** [What an attacker could achieve]
- **Recommendation:** [Specific fix with code example]

#### Summary Table

| # | Severity | Category | File | Description |
|---|----------|----------|------|-------------|
| 1 | CRITICAL | ... | ... | ... |

#### Recommendations

1. Immediate fixes (CRITICAL/HIGH)
2. Short-term improvements (MEDIUM)
3. Long-term hardening (LOW/INFO)
4. Tooling recommendations (NuGet audit, SAST integration, etc.)

## Severity

Use standard severity: CRITICAL > HIGH > MEDIUM > LOW > INFO. CRITICAL = actively exploitable, HIGH = significant with effort, MEDIUM = increased attack surface, LOW = minor improvement, INFO = hardening suggestion.

## Anti-Rationalization Table

| Rationalization | Reality |
|---|---|
| "This is just a test file" | Test code handling secrets or auth IS production-relevant. Report as INFO. |
| "Probably a false positive" | ALWAYS read surrounding code before dismissing. If you cannot prove it safe, report it. |
| "The framework handles this" | Verify the protection is actually enabled and configured. Defaults can be overridden. |
| "Internal API, not public-facing" | Internal APIs are attacked via SSRF, supply chain, lateral movement. |
| "No one would exploit this" | Threat models change. Report it; let the team decide risk acceptance. |

## Red Flags

STOP and investigate deeper if you encounter any of these:
- Any endpoint without an explicit auth attribute (`[Authorize]` or `[AllowAnonymous]`)
- Any `catch` block returning raw exception data to the client
- Any hardcoded key, token, password, or connection string literal
- Any `new HttpClient()` (should use `IHttpClientFactory`)
- Any `TypeNameHandling` value other than `None`

## Important Guidelines

1. Only report real findings with evidence (file path and line number). Do not speculate.
2. If a pattern search returns no results, note "No issues found" and move on.
3. For false positives (e.g., `System.Random` in tests, not production), note as INFO with explanation.
4. Prioritize production code over test code.
5. When reviewing "all", cap the report at the 30 most significant findings.
6. ALWAYS verify context before reporting -- a pattern match alone is not a finding. Read the surrounding code.
