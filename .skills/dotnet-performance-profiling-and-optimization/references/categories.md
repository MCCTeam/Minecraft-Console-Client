# Optimization Category Definitions

Complete check definitions for all 14 optimization categories. Each category lists specific checks to perform, with grep patterns at the end for automated scanning.

---

## Category 1: Memory Allocation (MEM)

Checks:

- **Unnecessary string allocations**: `Substring()` calls that could use `Span<char>` or `AsSpan()`. String concatenation with `+` inside loops (should use `StringBuilder` or `string.Create`).
- **Missing ArrayPool/MemoryPool usage**: `new byte[...]` for temporary buffers, especially in I/O paths. Should use `ArrayPool<byte>.Shared.Rent()` with try/finally Return.
- **Large Object Heap triggers**: Allocations of objects >= 85,000 bytes (arrays, large strings, `MemoryStream` without `RecyclableMemoryStream`).
- **Missing object pooling**: Frequently created/disposed objects (like `StringBuilder`) that could use `ObjectPool<T>`.
- **Record class vs record struct**: Small, immutable DTOs that are `record class` but could be `readonly record struct` to avoid heap allocation.
- **Boxing**: Value types cast to `object` or non-generic interfaces. Structs without `IEquatable<T>`.
- **Collection inefficiencies**: `new List<T>()` or `new Dictionary<K,V>()` without initial capacity when size is known or estimable. Double-lookup patterns (`TryGetValue` + indexer set) that could use `CollectionsMarshal.GetValueRefOrAddDefault`. Read-only dictionaries populated once that could be `FrozenDictionary<K,V>` (.NET 8+).
- **stackalloc for small buffers**: Flag `new byte[N]` where N <= 256 in synchronous methods. Recommend `Span<byte> buffer = stackalloc byte[N]` for short-lived stack allocation with zero GC pressure.
- **string.Create for pre-sized construction**: When output string length is known at call time, `string.Create(length, state, action)` avoids intermediate allocations by writing directly into the final buffer.
- **Interpolated strings in logging** *(Impact: LOW — only matters when the log level is inactive at runtime)*: `_logger.LogXxx($"...")` allocates the interpolated string even when the log level is disabled. Use structured logging parameters `_logger.LogXxx("Message {Param}", value)` or the `[LoggerMessage]` source generator for high-frequency hot paths.
- **ReadOnlySpan for string parsing**: Flag `.Split()` and `.Substring()` in hot paths where `AsSpan()` slicing avoids allocation. Common in string parsing, normalization, and URL handling.
- **CollectionsMarshal.GetValueRefOrAddDefault**: Flag the TryGetValue + indexer set double-lookup pattern. Single-lookup alternative reduces dictionary operations by 50%.
- **RecyclableMemoryStream**: Flag `new MemoryStream()` in I/O-heavy paths (blob upload/download, log writes). `Microsoft.IO.RecyclableMemoryStream` pools internal buffers and avoids LOH fragmentation.

Grep patterns:
```
\.Substring\(
new byte\[
new MemoryStream\(\)
new StringBuilder\(\)
new List<.*>\(\)
new Dictionary<.*>\(\)
_logger\.Log(Debug|Trace|Information|Warning|Error|Critical)\(\$"
\.Split\(
```

---

## Category 2: Async Anti-Patterns (ASYNC)

Checks:

- **Blocking async calls**: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` -- causes thread pool starvation. CRITICAL finding.
- **async void**: Methods declared `async void` (except event handlers) -- swallows exceptions and cannot be awaited.
- **Missing CancellationToken**: Async methods that do not accept or propagate `CancellationToken`. Every async method should have `CancellationToken cancellationToken = default`.
- **Missing ValueTask**: Methods that frequently return cached/synchronous results but use `Task<T>` instead of `ValueTask<T>`. Look for `if (cache.TryGetValue(...)) return Task.FromResult(...)`. Benchmark data: ValueTask 7.41ns/0B vs Task 15.23ns/72B.
- **Sequential awaits that could parallelize**: Multiple independent `await` calls in sequence that could use `Task.WhenAll`.
- **Async over sync**: Methods that use `Task.Run` to wrap synchronous code in an ASP.NET Core context (unnecessary and wastes a thread).
- **ConfigureAwait(false) in library projects**: LOW/INFO level. Not required in ASP.NET Core (no sync context), but recommended if library assemblies may be reused outside ASP.NET Core.
- **IAsyncEnumerable opportunities**: Methods returning `Task<List<T>>` where the caller iterates sequentially. If the caller processes items one-by-one, `IAsyncEnumerable<T>` reduces memory and improves time-to-first-byte.
- **Channel verification**: Verify `BoundedChannelOptions` has `SingleReader`/`SingleWriter` hints set for performance. Verify `CancellationToken` is propagated on `WriteAsync` and `ReadAllAsync`.

Grep patterns:
```
\.Result[^s]
\.Wait\(\)
\.GetAwaiter\(\)\.GetResult\(\)
async void
Task\.Run\(
\.WriteAsync\([^,]*\)
```

---

## Category 3: LINQ Inefficiencies (LINQ)

Checks:

- **Count() > 0 or Count() == 0**: Should use `Any()` or `!Any()`. `Count()` may enumerate the entire collection.
- **Multiple enumeration**: An `IEnumerable<T>` variable used more than once without materializing.
- **Filter after projection**: `.Select(...).Where(...)` -- should filter first, then project.
- **ToList() too early**: `.ToList().Where(...)` or `.ToList().Select(...)` -- materializes before filtering.
- **OrderBy before Where**: Sorting the full collection before filtering it down.

Grep patterns:
```
\.Count\(\) [><=!]
\.ToList\(\)\.Where\(
\.ToList\(\)\.Select\(
\.Select\(.*\)\.Where\(
\.OrderBy.*\.Where\(
```

---

## Category 4: Database (DB)

Check for both EF Core and CosmosDB patterns depending on what the project uses. Consult the project's CLAUDE.md or AGENTS.md for the data access strategy.

Checks:

- **N+1 queries**: Loops that call the database inside each iteration.
- **Missing AsNoTracking**: EF Core read-only queries without `.AsNoTracking()`.
- **Missing compiled queries**: Frequently executed EF Core queries on hot paths without `EF.CompileAsyncQuery`.
- **Full entity loading**: Fetching entire entities when only a few fields are needed (should project to DTOs).
- **Missing AsSplitQuery**: EF Core queries with multiple `.Include()` calls without `.AsSplitQuery()`.
- **CosmosDB partition key misuse**: Operations not specifying the partition key, or using cross-partition queries unnecessarily.
- **CosmosDB point reads**: Using queries instead of `ReadItemAsync` when both `id` and partition key are known.
- **RU cost awareness**: Flag discarded `ItemResponse<T>` without logging `RequestCharge`. Recommend tracking RU cost via metrics for cost visibility.
- **Cross-partition query detection**: `GetItemLinqQueryable()` without partition key option leads to fan-out queries. Verify all LINQ queryables specify the partition key.
- **Indexing policy review**: Flag if queries filter on fields that likely lack composite indexes.
- **Redundant round-trips**: Flag patterns where a query fetches an ID, then a separate point read fetches the full document. Recommend a single query.
- **EnableContentResponseOnWrite = false**: On write operations where the response body is not needed, setting this option reduces RU cost.

Grep patterns:
```
\.Include\(.*\.Include\(
await.*foreach.*await.*Async
ReadItemAsync
GetItemQueryIterator
GetItemLinqQueryable
\.RequestCharge
```

---

## Category 5: JSON Serialization (JSON)

Checks:

- **New JsonSerializerOptions per call**: `new JsonSerializerOptions { ... }` inside method bodies -- rebuilds the metadata cache every time. Should use a static readonly instance.
- **Missing source generators**: High-throughput serialization paths without `[JsonSerializable]` source generation context.
- **Newtonsoft.Json in hot paths**: If the project uses Newtonsoft.Json for MVC, flag any hot-path internal serialization that could benefit from `System.Text.Json` with source generators. NEVER suggest replacing the controller/DTO serializer without checking the project's documented constraints.
- **System.Text.Json source generators for internal serialization**: Internal paths (database serialization, audit logs, blob metadata) that don't affect API contracts are candidates for `System.Text.Json` with source generators.
- **JsonSerializerSettings singleton**: Flag `new JsonSerializerSettings()` in method bodies. The contract resolver cache is rebuilt each time. Use a static readonly instance or inject via DI.
- **CosmosDB SDK serializer**: The Cosmos SDK supports custom serializers. `CosmosSystemTextJsonSerializer` with source generators reduces allocation on read/write operations.

Grep patterns:
```
new JsonSerializerOptions
JsonConvert\.Serialize
JsonConvert\.Deserialize
new JsonSerializer
new JsonSerializerSettings
```

---

## Category 6: Caching (CACHE)

Checks:

- **Repeated expensive calls without caching**: Service methods that call external APIs on every request without caching the result.
- **Missing HybridCache pattern**: Look for manual cache-aside patterns that could use `HybridCache` for built-in stampede protection, two-level caching (L1 memory + L2 distributed), and tag invalidation. **HybridCache requires .NET 10 (or .NET 9) — the `Microsoft.Extensions.Caching.Hybrid` package does not target .NET 8.** On .NET 8 implement `IMemoryCache` (L1) + `IDistributedCache` (L2) manually with a `SemaphoreSlim` keyed by cache key for stampede protection. See [HybridCache GA announcement](https://devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga/).
- **Static data fetched repeatedly**: Configuration, taxonomies, or lookup data fetched from external APIs that rarely changes.
- **Missing output caching**: Read-only GET endpoints that return the same data for all callers -- candidates for `[OutputCache]`.
- **HybridCache upgrade path (.NET 10 only)**: Manual L1+L2 caching with `SemaphoreSlim` stampede protection can migrate to `HybridCache` `GetOrCreateAsync` with built-in stampede protection and tag invalidation **when the project target is `net10.0`**. On `net8.0` the manual pattern is the correct end state, not a stepping stone.
- **Cache stampede detection**: Cache-aside without locking -- `TryGetValue` followed by expensive call followed by `Set` without `SemaphoreSlim` or equivalent stampede guard.
- **Tag-based invalidation with RemoveByTagAsync**: When using HybridCache, group related entries by tag for efficient bulk invalidation instead of tracking individual keys.
- **IMemoryCache size limits**: Flag `AddMemoryCache()` without `SizeLimit` in `MemoryCacheOptions`. Unbounded in-memory cache can grow until the process runs out of memory.

Grep patterns:
```
GetAsync\(
SendAsync\(
_cache\.TryGetValue
DistributedCache
AddMemoryCache\(\)
```

---

## Category 7: DI Lifetime Issues (DI)

Consult the project's CLAUDE.md or AGENTS.md for the expected DI lifetime registrations.

Checks:

- **Transient services that should be Singleton**: Stateless, thread-safe services registered as Transient that have no per-request state (could be Singleton for zero allocation).
- **Scoped injected into Singleton**: A Scoped service captured in a Singleton constructor -- captive dependency bug.
- **IOptions vs IOptionsMonitor vs IOptionsSnapshot**: `IOptions<T>` in Singleton services that need to react to config changes should use `IOptionsMonitor<T>`. `IOptionsSnapshot<T>` in Singleton is a captive dependency.

---

## Category 8: Concurrency Issues (CONC)

Checks:

- **Lock contention**: `lock` statements that guard async operations (should use `SemaphoreSlim`).
- **Missing throttling**: Unbounded parallel calls to external APIs without `SemaphoreSlim` or concurrency limits.
- **Thread-unsafe patterns**: Shared mutable state without synchronization. `HttpContext` accessed from background threads.
- **Channel usage patterns**: If the project uses `Channel<T>` for background tasks, verify `SingleReader`/`SingleWriter` hints are set correctly for performance.

Grep patterns:
```
lock\s*\(
new SemaphoreSlim
HttpContext.*Task\.Run
```

---

## Category 9: HttpClient Misuse (HTTP)

Checks:

- **new HttpClient()**: Direct instantiation instead of `IHttpClientFactory`. Causes socket exhaustion and DNS caching issues.
- **Missing resilience**: HTTP calls without retry/circuit-breaker policies. Verify `Microsoft.Extensions.Http.Resilience` or Polly is applied to external API clients.
- **Missing response disposal**: `HttpResponseMessage` not disposed after reading.

Grep patterns:
```
new HttpClient\(
new HttpClient\b
```

---

## Category 10: Exception-Driven Control Flow (EXC)

Checks:

- **Try/catch for expected paths**: Using exceptions for normal control flow (e.g., catching `KeyNotFoundException` instead of `TryGetValue`, catching `FormatException` instead of `TryParse`).
- **Broad catch blocks**: `catch (Exception)` that swallow errors or use exceptions as branching logic.
- **Exception allocation in hot paths**: Throwing exceptions on paths that execute frequently.

Grep patterns:
```
catch\s*\(Exception\b
catch\s*\(KeyNotFoundException
catch\s*\(FormatException
catch\s*\(InvalidOperationException
```

---

## Category 11: Response Optimization (RESP)

Checks:

- **Missing compression**: No response compression middleware, or JSON responses served uncompressed.
- **Missing pagination**: Endpoints returning unbounded collections.
- **Missing ETags for conditional requests**: GET endpoints without ETag support where the data has a natural version (e.g., database ETags or row versions).

---

## Category 12: String Optimization (STR)

Checks:

- **String concatenation in loops**: `+=` on strings inside `for`/`foreach`/`while` loops.
- **String.Format in hot paths**: Could use interpolated string handlers or `StringBuilder`.
- **Repeated string operations**: Multiple `ToLower()`/`ToUpper()` calls on the same value. Should use `StringComparison.OrdinalIgnoreCase` instead.

Grep patterns:
```
\+= "
\+= \$"
\.ToLower\(\)
\.ToUpper\(\)
String\.Format\(
```

---

## Category 13: Startup & Pipeline Optimization (STARTUP)

Checks:

- **Middleware ordering**: Verify Program.cs follows the recommended sequence: ExceptionHandler, ResponseCompression, OutputCache, Routing, RateLimiter, CORS, Authentication, Authorization, MapControllers. Incorrect ordering degrades performance (e.g., compression after routing skips static responses).
- **Response compression**: Flag missing `AddResponseCompression`/`UseResponseCompression`. Without it, all JSON responses are uncompressed. Recommend Brotli (optimal ratio) + GZip (compatibility) providers with `EnableForHttps = true`.
- **Health check optimization**: `.ShortCircuit()` (.NET 8+) bypasses the entire middleware pipeline for health endpoints. `.DisableHttpMetrics()` prevents health check traffic from skewing request duration metrics.
- **PGO/ReadyToRun**: Check .csproj for `<TieredPGO>true</TieredPGO>` (dynamic PGO for runtime hot-path optimization) and `<PublishReadyToRun>true</PublishReadyToRun>` (pre-compiled code for faster startup). Both should be present for production builds.
- **Warm-up pattern**: `ApplicationStarted` callback to warm expensive singletons (database connections, cache, external API health). Cold-start latency without warm-up can spike P99 for the first requests after deployment.

Grep patterns:
```
UseResponseCompression
AddResponseCompression
ShortCircuit
AddOutputCache
UseOutputCache
TieredPGO
PublishReadyToRun
ApplicationStarted
```

---

## Category 14: Metrics & Observability (METRICS)

Checks:

- **IMeterFactory vs static new Meter()**: Services should inject `IMeterFactory` from DI rather than using `new Meter(...)`. `IMeterFactory` enables testability with `MetricCollector<T>` and proper meter lifecycle management.
- **Missing histograms**: If the project only has `Counter<long>` instruments, recommend `Histogram<double>` for request processing duration, external API latency, and database query duration. Histograms enable percentile analysis (P50/P95/P99).
- **Tag cardinality**: Verify metric tags have bounded cardinality. NEVER use request IDs, user IDs, or unbounded strings as metric tags. Tags like `operation_name`, `status_code`, `endpoint` are acceptable (bounded). Unbounded tags cause metric explosion and memory issues.
- **OpenTelemetry AddMeter() registration**: Verify custom meter names are registered with `.AddMeter("YourMeterName")` in the OpenTelemetry metrics configuration. Without this, custom counters are silently dropped.

Grep patterns:
```
new Meter\(
CreateCounter
CreateHistogram
AddMeter
\.Record\(
\.Add\(
```
