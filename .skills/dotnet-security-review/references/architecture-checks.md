# Architecture & Project-Specific Checks Reference
# Phase 3 checks for common ASP.NET Core project patterns. Read the project's CLAUDE.md
# or AGENTS.md for project-specific details (endpoint list, service names, DI registrations)
# to inform these checks.

## Check 1: Endpoint Auth Matrix Verification

Cross-reference the controller's actual `[Authorize]`/`[AllowAnonymous]` attributes against the project's documented auth requirements. Read CLAUDE.md or AGENTS.md for the expected auth matrix. Any mismatch is CRITICAL.

For projects with multiple auth schemes (e.g., Azure AD + custom JWT), verify each endpoint uses the correct scheme/policy.

## Check 2: Anonymous Endpoint Abuse Potential

For each `[AllowAnonymous]` endpoint, verify:
- Rate limiting or throttling exists for sensitive operations (e.g., code generation, login attempts)
- Enumeration attacks are mitigated (IDs are GUIDs or non-sequential, not auto-increment)
- No state modification without prior authentication or verification (e.g., OTP first)

## Check 3: OTP / MFA Security Review

If the project implements OTP or MFA, read the service implementation and verify:
- Code length is sufficient (6+ characters)
- Codes are generated with `RandomNumberGenerator`
- Hash is SHA-256 or stronger (not MD5/SHA1)
- Expiry is enforced (typically 5-10 minutes)
- Wrong attempt counter increments correctly and triggers lockout after a threshold
- No timing side-channel in hash comparison

## Check 4: Exception Handling Coverage

Grep for `throw new` statements. Verify that all thrown exceptions are either:
- The project's structured error type (e.g., `ApiException`, `DomainException`, or the project's custom base exception), OR
- Known typed exceptions for external service failures

Any unstructured exception thrown from handler/service code may bypass error filters and leak internal details.

## Check 5: Optimistic Concurrency on State Writes

If using a database with optimistic concurrency (ETags, row versions):
- Verify every write/update operation passes the concurrency token
- Verify the concurrency token store/tracking mechanism is consulted on every read/write cycle

## Check 6: Expired State Handling

If the project uses application-level state expiry (not DB TTL):
- Verify expired records are deleted or excluded on read (not returned to callers)
- Verify callers cannot act on expired data

## Check 7: Blob Storage SAS URL Security

If the project generates SAS URLs for blob storage:
- SAS token expiry is short-lived (minutes, not days)
- Permission is read-only (not write/delete)
- Scoped to the specific blob (not container-level)

## Check 8: Message Queue Security

If the project uses message queues (Service Bus, RabbitMQ, etc.):
- Messages do not contain secrets or unnecessary PII
- Queue connections use managed identity or connection strings from secret stores

## Check 9: JSON Serialization Settings

Check that `TypeNameHandling` is set to `None` (default) and not `Auto`/`All` anywhere. This applies to both Newtonsoft.Json and any custom serializer configuration.

## Check 10: Background Task Queue Safety

If the project uses a background task queue:
- Bounded capacity prevents unbounded memory growth
- Backpressure is handled correctly (not silently dropping critical events like audit logs)
- Task failures are observed and logged/metered

## Check 11: Custom Token / JWT Security

If the project issues its own JWTs (not just validating external tokens):
- **Algorithm**: HMAC-SHA256 or stronger (RSA for distributed validation)
- **Signing key source**: Key loaded from configuration/secret store, NOT hardcoded
- **Signing key length**: Minimum 256 bits (32 bytes) for HMAC-SHA256
- **Token expiry**: Appropriately capped (tokens should not outlive the session/resource they protect)
- **Claims validation**: Custom claims (e.g., resource IDs) are validated against route parameters by an authorization handler
- **TokenValidationParameters**: `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` all `true`
- **ClockSkew**: Tightened from default 5 minutes to 2 minutes or less

## Check 12: Response Data Sanitization

If the project sanitizes response data (e.g., stripping internal paths or fields):
- Sanitization handles malformed input gracefully (does not throw/crash)
- Only known sensitive fields are stripped (no over-stripping that breaks functionality)
- Sanitization is applied on every code path returning the data (not just the happy path)

## Check 13: Rate Limiting

Verify rate limiting posture:
- Grep for `AddRateLimiter` and `UseRateLimiter` -- if absent, note as finding
- Application-level throttling exists for sensitive operations (e.g., SMS/code generation resend limits)
- Brute force protection exists for verification endpoints (wrong attempt lockout)
- **Recommendation**: Add ASP.NET Core `System.Threading.RateLimiting` middleware for IP-based throttling on public endpoints

## Check 14: Security Headers Completeness

Check Program.cs / middleware for these headers (report missing ones):
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `X-XSS-Protection: 0` (disable legacy XSS filter; CSP is the modern replacement)
- `Content-Security-Policy` (at minimum for APIs: `default-src 'none'`)
- `Server` header removed
- `X-Powered-By` header removed

## Check 15: Middleware Pipeline Ordering

Read Program.cs and verify correct middleware order:
1. `UseExceptionHandler` (outermost -- catches everything)
2. `UseHsts` (non-development only)
3. `UseHttpsRedirection`
4. Security headers middleware
5. `UseRateLimiter` (if present)
6. `UseRouting` (if explicit)
7. `UseCors`
8. `UseAuthentication`
9. `UseAuthorization`
10. `MapControllers` / endpoints

Authentication MUST come before Authorization. CORS MUST come before Authentication. ExceptionHandler MUST be first.

## Check 16: NuGet Audit & Build Security

Check for build-level security configuration:
- Does `Directory.Build.props` exist? If so, verify `NuGetAudit`, `NuGetAuditMode`, `NuGetAuditLevel` settings.
- Are Roslyn security analyzer packages referenced? (`SecurityCodeScan.VS2019`, `SonarAnalyzer.CSharp`, `Meziantou.Analyzer`)
- Are `AnalysisLevel` / `AnalysisMode` set in `.csproj` or `Directory.Build.props`?
- Run Grep for floating versions: `Version="[^"]*\*"` in `.csproj` files
- Recommend `dotnet list package --vulnerable --include-transitive` as a CI step
