# Scanning Patterns Reference
# Automated Grep patterns organized by vulnerability class for Phase 1 scanning.
# Run all searches in parallel across .cs files in the target scope.

## Injection Vulnerabilities

| ID | Pattern | Target |
|----|---------|--------|
| INJ-1 | `\$".*SELECT\|INSERT\|UPDATE\|DELETE\|DROP\|EXEC` | SQL injection via string interpolation |
| INJ-2 | `string\.Format.*SELECT\|INSERT\|UPDATE\|DELETE` | SQL injection via string.Format |
| INJ-3 | `\.FromSqlRaw\(.*\$"\|\.FromSqlRaw\(.*string\.Format` | EF Core raw SQL injection |
| INJ-4 | `ExecuteSqlRaw\(.*\$"\|ExecuteSqlRaw\(.*string\.Format` | EF Core command injection |
| INJ-5 | `Process\.Start\|ProcessStartInfo` | Command injection |
| INJ-6 | `DirectorySearcher\|LdapConnection` | LDAP injection (check for string concat) |
| INJ-7 | `XmlDocument\|XmlReader\|XDocument` | XXE (verify secure settings) |
| INJ-8 | `Path\.Combine.*Request\|Path\.Combine.*user\|\.\.\/\|\.\.\\` | Path traversal |

## Insecure Deserialization

| ID | Pattern | Target |
|----|---------|--------|
| DES-1 | `BinaryFormatter\|SoapFormatter\|ObjectStateFormatter\|LosFormatter\|NetDataContractSerializer` | Banned deserializers |
| DES-2 | `JsonConvert\.DeserializeObject.*TypeNameHandling` | Newtonsoft type handling |
| DES-3 | `TypeNameHandling\s*=\s*TypeNameHandling\.(All\|Auto\|Objects\|Arrays)` | Unsafe type handling |

## Cryptography Weaknesses

| ID | Pattern | Target |
|----|---------|--------|
| CRY-1 | `new Random\(\)\|System\.Random` | Insecure randomness (should be RandomNumberGenerator) |
| CRY-2 | `MD5\.Create\|SHA1\.Create\|DESCryptoServiceProvider\|RC2CryptoServiceProvider\|TripleDES` | Weak algorithms |
| CRY-3 | `ECB` | Insecure cipher mode |
| CRY-4 | `password\|secret\|key\|token\|credential\|apikey\|connectionstring` in string literals | Hardcoded secrets |

## Async Anti-Patterns

| ID | Pattern | Target |
|----|---------|--------|
| ASY-1 | `\.Result[^s]\|\.Result$` | Sync-over-async deadlock risk |
| ASY-2 | `\.Wait\(\)` | Sync-over-async deadlock risk |
| ASY-3 | `\.GetAwaiter\(\)\.GetResult\(\)` | Sync-over-async |
| ASY-4 | `Task\.Run\(` | Thread pool abuse in ASP.NET context |

## Sensitive Data Exposure

| ID | Pattern | Target |
|----|---------|--------|
| EXP-1 | `_logger\.Log.*password\|_logger\.Log.*secret\|_logger\.Log.*token\|_logger\.Log.*apiKey` (case insensitive) | Logging secrets |
| EXP-2 | `Console\.Write.*password\|Console\.Write.*secret\|Console\.Write.*token` | Console output of secrets |
| EXP-3 | `Html\.Raw\(` | XSS via unencoded HTML |
| EXP-4 | `Exception\.ToString\(\)\|Exception\.StackTrace\|Exception\.Message` returned in HTTP responses | Stack trace leakage |

## SSRF Risks

| ID | Pattern | Target |
|----|---------|--------|
| SSRF-1 | `new HttpClient\(\).*\+\|HttpClient.*GetAsync\(.*\+\|HttpClient.*PostAsync\(.*\+` | User-controlled URLs |
| SSRF-2 | `new Uri\(.*Request\|new Uri\(.*user\|new Uri\(.*input` | Unvalidated URI construction |
| SSRF-3 | `HttpClient.*GetAsync\(.*[^"]\)\|HttpClient.*PostAsync\(.*[^"]\)` | Non-literal URLs in HTTP calls |
| SSRF-4 | `new Uri\([^"]*\)` | Dynamic URI construction |
| SSRF-5 | `IPAddress\.Parse\("\|Uri\("http` | Hardcoded IPs/URLs |

## Missing Security Controls

| ID | Pattern | Target |
|----|---------|--------|
| CTL-1 | `\[HttpPost\]\|\[HttpPut\]\|\[HttpDelete\]\|\[HttpPatch\]` | Unannotated endpoints (check for nearby [Authorize]/[AllowAnonymous]) |
| CTL-2 | `AllowAnyOrigin` | CORS misconfiguration |
| CTL-3 | `app\.UseDeveloperExceptionPage` | Dev error page in production |
| CTL-4 | `#pragma warning disable` | Disabled security warnings |

## ReDoS

| ID | Pattern | Target |
|----|---------|--------|
| REG-1 | `new Regex\((?!.*RegexOptions\.NonBacktracking)` | Regex without NonBacktracking (ReDoS risk in .NET 7+) |

## Log Injection

| ID | Pattern | Target |
|----|---------|--------|
| LOG-1 | `_logger\.Log.*(Request\.Query\|Request\.Form\|Request\.Headers\|Request\.Body)` | Unsanitized request data in logs |
| LOG-2 | `_logger\.Log.*\\n\|_logger\.Log.*\\r` | Newline chars in log messages (log forging) |

## Open Redirect

| ID | Pattern | Target |
|----|---------|--------|
| RED-1 | `Redirect\(\|RedirectToAction\(.*\+` | Open redirect via concatenation |
| RED-2 | `Response\.Redirect\(` | Direct response redirect |

## Cookie Security

| ID | Pattern | Target |
|----|---------|--------|
| COK-1 | `CookieOptions\|\.Cookies\.Append` | Cookie usage (verify HttpOnly, Secure, SameSite) |
| COK-2 | `SameSite\s*=\s*SameSiteMode\.None` | SameSite=None (requires Secure flag) |

## File Upload

| ID | Pattern | Target |
|----|---------|--------|
| UPL-1 | `IFormFile` | File upload handling (verify validation) |
| UPL-2 | `ContentType.*application/octet-stream\|ContentType.*\*\/\*` | Permissive content type acceptance |

## Claims Safety

| ID | Pattern | Target |
|----|---------|--------|
| CLM-1 | `User\.Claims\.First\(\|User\.FindFirst\(.*\.Value(?!\?)` | Null-unsafe claims access (missing ?.) |

## Thread Safety

| ID | Pattern | Target |
|----|---------|--------|
| THR-1 | `static\s+.*HttpClient\s+\w+\s*=\s*new\s+HttpClient` | Static HttpClient instantiation (use IHttpClientFactory) |
