# Deep Review Categories Reference
# File-by-file review checklist for Phase 2. For each file in scope (or top ~20 most
# security-relevant files when reviewing "all"), read the file and check each applicable category.

## Category A: Authentication & Authorization

1. Every controller action has either `[Authorize]` (class or method level) or `[AllowAnonymous]` explicitly.
2. No IDOR: when accessing resources by ID, verify the handler checks that the caller owns or is authorized to access that resource.
3. JWT validation settings are strict: issuer, audience, lifetime, algorithm all validated.
4. Token acquisition uses correct flow: app tokens for backend-to-backend, OBO only where user context is needed.
5. No `[AllowAnonymous]` on endpoints that modify sensitive state without alternative authentication (e.g., OTP verification first).
6. **Multiple auth scheme verification**: If the project uses multiple auth schemes (e.g., Azure AD + custom JWT), verify correct scheme is applied per endpoint. No scheme confusion between internal and client-facing endpoints.
7. **JWT `alg:none` rejection**: Verify `TokenValidationParameters` does NOT allow `alg:none`. All schemes must validate the signing algorithm (`ValidateIssuerSigningKey = true`).
8. **HMAC signing key minimum length**: If using HMAC-SHA256 for JWT signing, the key must be at least 256 bits (32 bytes). Check options validation.
9. **Structured error responses on auth failure**: `OnChallenge` (401) and `OnForbidden` (403) events should return structured JSON error responses, not default HTML/empty responses.

## Category B: Input Validation

1. All DTOs accepted by handlers have corresponding FluentValidation validators registered.
2. Route parameters are validated for format before use (e.g., GUID format, positive integers).
3. File uploads are validated for content type, size, and extension (not just extension).
4. No unvalidated user input flows into file paths, URLs, SQL, commands, or log messages.
5. Phone numbers, emails, and other PII are validated and normalized before processing.

## Category C: Error Handling & Information Leakage

1. All expected errors use a structured error type -- never return raw exception details to clients.
2. Exception filters catch known exception types and return only safe error payloads.
3. Unknown exceptions are wrapped as generic 500 errors without stack traces or internal details.
4. Error messages returned to clients do not reveal internal architecture, database schema, or file paths.
5. Catch blocks never silently swallow exceptions -- they must log or rethrow.

## Category D: Cryptography & Secrets

1. OTP/MFA codes use `RandomNumberGenerator` (not `System.Random`).
2. Hash comparison uses constant-time comparison to prevent timing attacks.
3. Hash storage uses a secure algorithm (SHA-256 minimum; bcrypt/Argon2 for passwords).
4. No secrets, connection strings, or API keys appear in source code or `appsettings.json` committed to git.
5. Options validation (`ValidateOnStart()`) is configured to reject placeholder secrets in production.

## Category E: Data Protection & PII

1. Sensitive fields (phone numbers, etc.) are masked before returning to unauthenticated callers.
2. PII (names, addresses, phone numbers, emails) is not logged in full -- use masking.
3. Sensitive internal fields (hash values, internal IDs) are excluded from API responses.
4. Blob/file storage SAS URLs have appropriate expiry times and permissions (read-only, short-lived).
5. Audit logs do not contain raw PII that violates data protection requirements.

## Category F: Concurrency & State Safety

1. Database state mutations use optimistic concurrency (ETags, row versions, or equivalent).
2. Concurrency exceptions are caught and retried appropriately in handlers.
3. Multi-step validation flows (OTP, MFA) handle concurrent attempts correctly.
4. Counter increments (e.g., wrong attempt counts) are atomic or protected against race conditions.
5. Scheduled/delayed operations do not race with in-progress workflows.

## Category G: CancellationToken Propagation

1. Every `async` method in the call chain accepts `CancellationToken cancellationToken = default`.
2. The token is passed to every awaited call: HTTP calls, DB queries, blob operations, queue sends.
3. The controller passes `HttpContext.RequestAborted` to handlers.
4. Missing propagation is a DoS vector (abandoned requests hold resources).

## Category H: HTTP Client Security

1. HttpClient instances have timeouts configured (not infinite).
2. Delegating handlers do not log tokens or authorization headers.
3. SSL/TLS validation is not disabled (`ServerCertificateCustomValidationCallback` returning true).
4. Retry policies do not retry on authentication failures (401/403).
5. External API clients have adequate timeout and error handling even without resilience middleware.
6. **Standard resilience handler**: Verify `AddStandardResilienceHandler()` or equivalent resilience pipeline is configured on named HttpClients.
7. **Retry-After header respect**: Retry policies should honor `Retry-After` headers from downstream APIs to avoid cascading failures.
8. **DNS refresh**: Verify `SocketsHttpHandler.PooledConnectionLifetime` is set (recommended 2-5 min) to handle DNS changes.

## Category I: Configuration Security

1. CORS policy does not use `AllowAnyOrigin()` in production.
2. Swagger UI is disabled in production (or restricted to authorized users).
3. Health check endpoints do not expose sensitive information.
4. `X-Powered-By` and `Server` headers are removed.
5. HTTPS redirection and HSTS are configured for production.

## Category J: Logging & Monitoring Security

1. Authentication events are logged (both success and failure) for audit trail.
2. Authorization failures are logged with sufficient context (user, endpoint, reason).
3. Input validation failures are logged (not just returned as 400 responses).
4. Structured logging used throughout -- no string interpolation in log method calls (use message templates).
5. Sensitive data (passwords, tokens, PII, hash values) is NEVER logged at any log level.
6. Correlation IDs are included in all error log entries for traceability.
7. Log output is not accessible to API clients (no endpoint returns log data).

## Category K: Output Encoding & Response Security

1. No internal file paths, class names, or assembly names leak in API responses (check error messages, headers).
2. Razor templates are verified for `@Html.Raw()` usage -- must be justified and input-sanitized.
3. `TypeNameHandling.None` verified for Newtonsoft.Json serialization (prevents type injection).
4. `Content-Type` headers are explicitly set on all responses (no browser MIME-sniffing).
5. Response sanitization logic handles malformed input gracefully (no crashes on invalid JSON/data).

## Category L: Supply Chain & Build Security

1. `Directory.Build.props` exists with NuGet audit settings (`NuGetAudit`, `NuGetAuditMode`, `NuGetAuditLevel`).
2. Package versions are pinned (no floating versions like `Version="1.*"`).
3. `AnalysisLevel` and `AnalysisMode` are set to `latest-Recommended` / `Recommended` in build configuration.
4. Security analyzers included in packages (SecurityCodeScan, SonarAnalyzer.CSharp, or Meziantou.Analyzer).
5. No known-vulnerable version ranges in `.csproj` files (check Newtonsoft.Json >= 13.0.1, Microsoft.Identity.Web >= 2.x, System.Text.Json >= 8.0.5).
