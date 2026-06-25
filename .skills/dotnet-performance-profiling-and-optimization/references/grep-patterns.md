# Anti-Pattern Grep Library

Consolidated grep patterns for automated scanning. Run these with the Grep tool using `type: "cs"` filter.

All patterns use ripgrep regex syntax. Run during Phase 1 (Discovery) broad scan.

> **See also:** each topic catalog ([critical-patterns.md](critical-patterns.md), [async-patterns.md](async-patterns.md), [memory-and-strings.md](memory-and-strings.md), [collections-and-linq.md](collections-and-linq.md), [regex-patterns.md](regex-patterns.md), [io-and-serialization.md](io-and-serialization.md), [structural-patterns.md](structural-patterns.md)) has its own Detection section with topic-specific recipes and ratio-counting guidance. This file is the consolidated cross-cutting library; load topic files when their signals are present.

---

## ASYNC anti-patterns (CRITICAL)

```
\.Result\b
\.Wait\(\)
\.GetAwaiter\(\)\.GetResult\(\)
async void\b
Task\.Run\(
\.WriteAsync\([^,]*\)
```

**False positive note**: `.Result` matches `Task.FromResult` -- verify actual blocking before flagging.

---

## Memory anti-patterns (MEM)

```
new byte\[\d{4,}\]
new byte\[
new MemoryStream\(\)
\.Substring\(
new StringBuilder\(\)
new List<.*>\(\)
new Dictionary<.*>\(\)
_logger\.Log(Debug|Trace|Information|Warning|Error|Critical)\(\$"
\.Split\(
```

---

## LINQ anti-patterns

```
\.Count\(\)\s*[><=!]
\.ToList\(\)\.Where\(
\.ToList\(\)\.Select\(
\.Select\(.*\)\.Where\(
\.OrderBy.*\.Where\(
```

---

## Database / CosmosDB anti-patterns (DB)

```
\.Include\(.*\.Include\(
await.*foreach.*await.*Async
ReadItemAsync
GetItemQueryIterator
GetItemLinqQueryable
\.RequestCharge
```

---

## JSON anti-patterns

```
new JsonSerializerOptions
new JsonSerializerSettings
JsonConvert\.Serialize
JsonConvert\.Deserialize
new JsonSerializer
```

---

## Caching anti-patterns (CACHE)

```
GetAsync\(
SendAsync\(
_cache\.TryGetValue
DistributedCache
AddMemoryCache\(\)
```

---

## HttpClient misuse (HTTP)

```
new HttpClient\(
new HttpClient\b
```

---

## Exception control flow (EXC)

```
catch\s*\(Exception\b
catch\s*\(KeyNotFoundException
catch\s*\(FormatException
catch\s*\(InvalidOperationException
```

---

## String anti-patterns (STR)

```
\+= "
\+= \$"
\.ToLower\(\)
\.ToUpper\(\)
String\.Format\(
```

---

## Concurrency anti-patterns (CONC)

```
lock\s*\(
new SemaphoreSlim
HttpContext.*Task\.Run
```

---

## Startup & Pipeline (STARTUP)

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

## Metrics & Observability (METRICS)

```
new Meter\(
CreateCounter
CreateHistogram
AddMeter
```

---

## CancellationToken coverage

```
async Task[<\s].*\)\s*$
```

This pattern finds async methods whose signature ends without a CancellationToken parameter. Verify each match -- some may be interface implementations where the token is propagated differently.
