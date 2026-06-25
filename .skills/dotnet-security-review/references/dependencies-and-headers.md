# Dependencies & Headers Reference
# Combined Phase 4 (dependency vulnerability checks) and Phase 5 (headers/middleware) content.

## Phase 4: Dependency Vulnerability Check

### Automated Grep Checks

Run these Grep patterns against `.csproj` files to detect known-vulnerable version ranges:

| Pattern | Risk |
|---------|------|
| `Newtonsoft\.Json.*Version="([0-9]+)` where major < 13 | CVEs in Newtonsoft.Json < 13.0.1 |
| `Newtonsoft\.Json.*Version="13\.0\.0"` | Pre-patch 13.x |
| `Microsoft\.Identity\.Web.*Version="1\."` | CVEs in Microsoft.Identity.Web < 2.x |
| `System\.Text\.Json.*Version="[0-7]\.\|Version="8\.0\.[0-4]"` | CVEs in System.Text.Json < 8.0.5 |
| `Version="[^"]*\*"` | Floating versions (unpinned, supply chain risk) |

### NuGet Audit Configuration Check

Grep `Directory.Build.props` and `.csproj` files for:
- `<NuGetAudit>true</NuGetAudit>` -- should be present
- `<NuGetAuditMode>all</NuGetAuditMode>` -- audits transitive dependencies
- `<NuGetAuditLevel>low</NuGetAuditLevel>` -- catches all severity levels
- `<WarningsAsErrors>` containing `NU1903;NU1904` -- fails build on high/critical vulnerabilities

### ReDoS in Validators

Check all `Regex` and `.Matches()` calls in validators:
- Pattern: `new Regex\((?!.*RegexOptions\.NonBacktracking)` -- missing NonBacktracking flag (.NET 7+)
- Check for nested quantifiers: `(a+)+`, `(a*)*`, `(a|a)*` patterns

### Command Recommendation

Include in report output (do NOT run automatically):
```bash
dotnet list package --vulnerable --include-transitive
```

## Phase 5: Security Headers & Middleware Pipeline

### Headers Checklist (14 items)

Read `Program.cs` and any middleware configuration files. Check for each header:

| # | Header / Control | Expected Value | Severity if Missing |
|---|-----------------|----------------|---------------------|
| 1 | `X-Content-Type-Options` | `nosniff` | MEDIUM |
| 2 | `X-Frame-Options` | `DENY` | MEDIUM |
| 3 | `Referrer-Policy` | `strict-origin-when-cross-origin` | LOW |
| 4 | `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | LOW |
| 5 | `X-XSS-Protection` | `0` (disable legacy filter; CSP replaces it) | LOW |
| 6 | `Content-Security-Policy` | At minimum `default-src 'none'` for APIs | MEDIUM |
| 7 | `Server` header | REMOVED | LOW |
| 8 | `X-Powered-By` header | REMOVED | LOW |
| 9 | `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` | HIGH |
| 10 | `Cache-Control` | `no-store` on sensitive data endpoints | MEDIUM |
| 11 | HTTPS Redirection | `app.UseHttpsRedirection()` present | HIGH |
| 12 | HSTS | `app.UseHsts()` in non-development | HIGH |
| 13 | Rate Limiting | `app.UseRateLimiter()` present | MEDIUM |
| 14 | Swagger restricted | Conditionally enabled (dev/staging only) | MEDIUM |

### Middleware Pipeline Ordering

The correct order for ASP.NET Core middleware is critical. Misordering can bypass security controls.

**Expected order:**
```
1. app.UseExceptionHandler(...)     // Outermost: catches all unhandled exceptions
2. app.UseHsts()                    // HSTS (non-development only)
3. app.UseHttpsRedirection()        // Force HTTPS
4. Security headers middleware       // Custom: X-Content-Type-Options, etc.
5. app.UseRateLimiter()             // Throttle before routing (if present)
6. app.UseRouting()                 // (implicit in .NET 8+ with MapControllers)
7. app.UseCors(...)                 // CORS before auth (preflight must not require auth)
8. app.UseAuthentication()          // Identify the caller
9. app.UseAuthorization()           // Enforce access rules
10. app.MapControllers()            // Endpoint dispatch
```

**Critical ordering rules:**
- `UseExceptionHandler` MUST be first -- otherwise exceptions in early middleware are unhandled
- `UseAuthentication` MUST come before `UseAuthorization` -- otherwise auth policies have no identity to check
- `UseCors` MUST come before `UseAuthentication` -- otherwise CORS preflight (OPTIONS) requests fail with 401
- `UseRateLimiter` SHOULD come before `UseRouting` -- otherwise rate limits apply after route matching overhead
- `UseHsts` and `UseHttpsRedirection` SHOULD come early -- before any response body is written

### Middleware Verification Procedure

1. Read `Program.cs` from the `var app = builder.Build()` line to `app.Run()`
2. List every `app.Use*` and `app.Map*` call in order
3. Compare against expected order above
4. Report any misordering as MEDIUM severity
