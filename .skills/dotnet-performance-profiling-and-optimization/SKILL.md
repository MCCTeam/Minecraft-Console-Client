---
name: dotnet-performance-profiling-and-optimization
description: >-
  Use when a .NET process is slow, hung, memory-heavy, or deadlocked, or when
  analyzing C#/ASP.NET Core code for performance anti-patterns across memory,
  async, LINQ, database, JSON, caching, DI, concurrency, HttpClient, exceptions,
  response, strings, startup, and metrics.
metadata:
  category: technique
  triggers:
    - dotnet-counters
    - dotnet-trace
    - dotnet-dump
    - dotnet-gcdump
    - dotnet-stack
    - benchmarkdotnet
    - allocations
    - gc pressure
    - memory leak
    - hot path
    - linq
    - stackalloc
    - span
    - boxing
    - heap
    - latency
    - throughput
    - slow
    - hang
    - deadlock
    - optimize
    - performance
    - async
    - caching
    - di lifetime
    - ef core
    - cosmosdb
    - json serialization
    - httpclient
    - middleware
version: 1.1.0
platform: ".NET 8 and .NET 10 (no .NET 9 projects in scope)"
---

# .NET Performance: Diagnostic & Code Review

Unified C#/.NET performance skill targeting **.NET 8 and .NET 10**. Two modes: live process diagnostics (Mode A) and static code optimization review with fixes (Mode B).

## Step 0 — Detect the target framework

Before recommending APIs, follow `../../references/detect-target-framework.md`. Many .NET 9+ APIs (`HybridCache`, `MemoryExtensions.Split` for spans, `Dictionary.GetAlternateLookup`, `params ReadOnlySpan<T>`) do **not** exist on .NET 8 — the references below mark the floor for each pattern, and you must downgrade to the .NET 8 fallback when the target is `net8.0`. Stephen Toub's posts are the primary benchmark source:
[Performance Improvements in .NET 8](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/) ·
[Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/).

## References

Load on demand:
- [references/memory-model-gc.md](references/memory-model-gc.md) — stack vs heap, generations, LOH, boxing, GC tuning
- [references/code-patterns.md](references/code-patterns.md) — LINQ, Span/Memory, stackalloc, structs, boxing, pooling, strings (conceptual framing)
- [references/categories.md](references/categories.md) — 14 optimization category definitions with checks and grep patterns (Mode B spine)
- [references/grep-patterns.md](references/grep-patterns.md) — consolidated anti-pattern grep library for Phase 1 scanning
- [references/measurement-guide.md](references/measurement-guide.md) — BenchmarkDotNet, k6, dotnet-counters, KPI targets, CI/CD

Pattern catalogs (with measured impact numbers, ❌/✅ pairs, and per-topic Detection recipes):
- [references/critical-patterns.md](references/critical-patterns.md) — 17 🔴 patterns: deadlocks, order-of-magnitude regressions, excessive allocations
- [references/async-patterns.md](references/async-patterns.md) — sync-over-async, ValueTask hot paths, Channels, false sharing
- [references/memory-and-strings.md](references/memory-and-strings.md) — `u8` literals, `Span.Split`/`TryWrite`, compound `+=`, chained `.Replace()`
- [references/collections-and-linq.md](references/collections-and-linq.md) — `FrozenDictionary`, `GetAlternateLookup`, `CollectionsMarshal.GetValueRefOrAddDefault`, hoisting static data
- [references/regex-patterns.md](references/regex-patterns.md) — `[GeneratedRegex]`, `IsMatch`, `EnumerateMatches`, `NonBacktracking`
- [references/io-and-serialization.md](references/io-and-serialization.md) — `HttpCompletionOption.ResponseHeadersRead`, `useAsync` `FileStream`, `Memory<byte>` overloads
- [references/structural-patterns.md](references/structural-patterns.md) — sealed-class devirtualization (absence pattern, scale-based severity)

Reference loading guide for Mode B by signal:

| Signal in Code | Load |
|---|---|
| `async`, `await`, `Task`, `ValueTask` | `async-patterns.md` |
| `Span<`, `Memory<`, `stackalloc`, `string.Substring`, `+=` in loops, `params` | `memory-and-strings.md` |
| `Regex`, `[GeneratedRegex]`, `Regex.Match`, `RegexOptions.Compiled` | `regex-patterns.md` |
| `Dictionary<`, `List<`, `.ToList()`, LINQ chains, `static readonly Dictionary<` | `collections-and-linq.md` |
| `JsonSerializer`, `HttpClient`, `Stream`, `FileStream` | `io-and-serialization.md` |
| Any code review on a hot path | always check `critical-patterns.md` first |
| Codebase-wide scans (sealed classes, static `Dictionary` → `FrozenDictionary`) | `structural-patterns.md` |

## Iron Rule

**Always measure first, change second, and re-measure third.**

Never claim an optimization without before/after evidence from the same scenario.

| Rationalization | Reality |
|---|---|
| "This is obviously slow" | The runtime, JIT, and libraries often invalidate intuition. |
| "struct means stack" | Value types are stored inline — not always on the stack. |
| "All LINQ is slow" | .NET 9+ improved many LINQ paths. Measure before rewriting. |
| "GC.Collect will fix it" | Forced collection treats symptoms, not cause. |
| "Too small to matter" | MEDIUM+ impact is cumulative across the request pipeline. |
| "I'll change the DI lifetime while I'm here" | DI lifetime changes require explicit user approval. |
| "Need to refactor to optimize" | Optimization fixes must be surgical. Refactoring is a separate task. |
| "Tests pass so fix is correct" | Tests passing = behavior preserved. Still verify the metric improved. |

## Mode Selection

| Situation | Mode |
|---|---|
| Live process: slow, high CPU/memory, hung, deadlocked, GC pauses | **A – Diagnostic** |
| Asking how GC, heap, boxing, or LINQ overhead works in .NET | **A – Diagnostic** (conceptual) |
| Code to analyze for anti-patterns, then fix | **B – Code Review** |
| Both a running process AND code to fix | Start with **A**, then **B** on hot paths identified |

**Not for:** Visual Studio, Rider, PerfView, speedscope, or GUI-first workflows.

---

## Mode A: Diagnostic (Live Process)

### Investigation Order

1. **`dotnet-counters`** — always start here for live triage.
2. **`dotnet-stack`** — immediately if process is stuck, hung, or deadlocked.
3. **`dotnet-trace`** — if CPU or allocation hot paths matter.
4. **`dotnet-gcdump`** — if heap growth matters more than call paths.
5. **`dotnet-dump`** — if SOS heap inspection or postmortem analysis is needed.
6. After live evidence identifies a candidate routine, apply patterns from [references/code-patterns.md](references/code-patterns.md) and [references/memory-model-gc.md](references/memory-model-gc.md).
7. Use BenchmarkDotNet if the change is isolated and needs microbenchmark comparison.
8. Re-run the original live capture to prove the real workload improved.

### CLI Tool Selection

| Question | Tool | What it answers |
|---|---|---|
| Is the process allocating, GCing, or saturating CPU? | `dotnet-counters` | Live counters and trend direction |
| Is the process hung or deadlocked right now? | `dotnet-stack` | Current managed stack snapshot |
| Which call paths consume CPU or allocate heavily? | `dotnet-trace` | Sampled execution and runtime events |
| Which object types dominate managed heap? | `dotnet-gcdump` | Heap composition and type totals |
| Need SOS heap inspection or thread state? | `dotnet-dump` | Full dump plus CLI analysis |
| Did a code change improve an isolated routine? | BenchmarkDotNet | Reproducible microbenchmark comparison |

### Minimal CLI Commands

```bash
dotnet-counters monitor -p <PID> --counters System.Runtime
dotnet-counters monitor -n <ProcessName> --counters System.Runtime,Microsoft.AspNetCore.Hosting
dotnet-stack report -p <PID>
dotnet-trace collect -p <PID> --duration 00:00:30
dotnet-trace report <trace.nettrace> topN
dotnet-gcdump collect -p <PID>
dotnet-gcdump report <file.gcdump>
dotnet-dump collect -p <PID> --type Heap
dotnet-dump analyze <dump> -c "dumpheap -stat" -c "exit"
```

Minimal BenchmarkDotNet pattern:
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CandidateBench
{
    [Benchmark(Baseline = true)]
    public int Original() => OriginalImpl();

    [Benchmark]
    public int Candidate() => CandidateImpl();
}
```
```bash
dotnet run -c Release
```

### Reference Loading Guide

| User question | Load first |
|---|---|
| "How do stack and heap really work in .NET?" | `references/memory-model-gc.md` |
| "Why is GC pausing or why is LOH churn hurting us?" | `references/memory-model-gc.md` |
| "How should I optimize this LINQ?" | `references/code-patterns.md` |
| "Can I move this to the stack with stackalloc or Span?" | `references/code-patterns.md` |
| "Should this be a struct, ref struct, readonly struct, or class?" | Both |
| "Why is this boxing?" | `references/code-patterns.md` |

### Diagnostic Output

Report: measured symptom + evidence (counter values, trace hotspots, heap stats) · chosen tool and why · relevant tradeoff (allocation vs copy, deferred vs eager, stack vs pool) · before/after result, or explicitly state if still unverified.

---

## Mode B: Code Review (Static Analysis)

### Target

`$ARGUMENTS` is the optimization target:
- **File path**: Analyze that file and its close dependencies.
- **Directory**: Analyze all C# files in that directory.
- **"all"**: Scan the solution with Grep, deep-dive the worst offenders.
- **`--fix`** anywhere: Skip confirmation and apply fixes after analysis.
- **Empty**: Check `git diff --name-only HEAD~5 -- '*.cs'` for recently changed files. If none, ask the user.

### Phase 1: Discovery

1. **Glob** to find `.cs` files matching the target.
2. **Read** file contents. For files under 500 lines, read the whole file first — visual inspection catches patterns faster than grep, then grep confirms counts.
3. **Detect signals** in the code (async, Span, Regex, Dictionary, JsonSerializer, etc.) and **load matching pattern catalogs** from the per-topic references listed at the top of this file.
4. **Grep** for anti-patterns. Run the recipes in [references/grep-patterns.md](references/grep-patterns.md) plus the per-topic Detection sections in the catalogs you loaded.
5. **Emit a scan execution checklist** before classifying — list each recipe and the hit count. **0 hits is valid and valuable** (confirms good practice).

### Phase 2: Analysis (Read-Only)

Check each file against all 14 categories. Record per finding: **file path, line number, current pattern, recommended pattern, impact level, category**.

Read [references/categories.md](references/categories.md) for detailed check definitions.

#### Compound Allocation Check

Single-line grep recipes miss multi-allocation patterns. After running scan recipes, look for:

1. **Branched `.Replace()` chains** — methods that call `.Replace()` across multiple `if/else` branches. Report total allocation count across all branches, not just per-line.
2. **Cross-method chaining** — public method A calls B (which does 3 regex replaces) then calls C (which allocates). Report the total chain cost as one finding, not per-method.
3. **Compound `+=` with embedded allocating calls** — `result += $"...{Foo().ToLower()}"` is 2+ allocations (interpolation + `ToLower` + concatenation). Flag the compound cost, not just `.ToLower()`.
4. **`string.Format` specificity** — distinguish resource-loaded format strings (not fixable) from compile-time literal format strings (fixable with interpolation). Enumerate only the actionable sites.

#### Cross-File Consistency Check

If an optimized pattern is found in one file, check whether sibling files (same directory, same interface, same base class) use the un-optimized equivalent. Flag as MEDIUM with the optimized file as evidence.

#### Verify-the-Inverse Rule

For absence patterns (e.g., unsealed classes, static `Dictionary` not converted to `FrozenDictionary`, `RegexOptions.Compiled` not migrated to `[GeneratedRegex]`), always count both sides and report the **N-of-M ratio**, not just the count of bad cases. The ratio determines severity:

- 0/185 sealed → systematic codebase-wide issue
- 12/15 sealed → consistency fix on the remaining 3
- 50/100 sealed → mid-migration; flag the laggards

| # | Category | Code | Focus |
|---|---|---|---|
| 1 | Memory Allocation | MEM | Span, ArrayPool, pooling, stackalloc, string optimization, collections |
| 2 | Async Anti-Patterns | ASYNC | Blocking, ValueTask, CancellationToken, IAsyncEnumerable, Channel |
| 3 | LINQ Inefficiencies | LINQ | Count vs Any, multiple enumeration, filter/project order |
| 4 | Database | DB | EF Core, CosmosDB patterns, N+1, partition keys, RU cost |
| 5 | JSON Serialization | JSON | Options reuse, source generators, serializer boundaries |
| 6 | Caching | CACHE | HybridCache, stampede protection, output cache, size limits |
| 7 | DI Lifetimes | DI | Captive dependencies, lifetime mismatches, IOptions patterns |
| 8 | Concurrency | CONC | Lock contention, throttling, thread safety, Channel patterns |
| 9 | HttpClient | HTTP | IHttpClientFactory, resilience, response disposal |
| 10 | Exception Control Flow | EXC | Try/catch for expected paths, broad catches |
| 11 | Response Optimization | RESP | Compression, pagination, ETags |
| 12 | String Optimization | STR | Concatenation loops, ToLower/ToUpper, String.Format |
| 13 | Startup & Pipeline | STARTUP | Middleware ordering, compression, health checks, PGO |
| 14 | Metrics & Observability | METRICS | IMeterFactory, histograms, tag cardinality, OpenTelemetry |

### Phase 3: Report

```
## Performance Analysis Report

### Summary
- Files analyzed: N
- Total findings: N
- Critical (HIGH): N | Moderate (MEDIUM): N | Minor (LOW): N

### Findings by Category

#### [CATEGORY_NAME] (N findings)

| # | Impact | File:Line | Issue | Recommendation |
|---|--------|-----------|-------|----------------|
| 1 | HIGH   | `path/File.cs:42` | Current anti-pattern | Recommended fix |

### Prioritized Action List
1. [HIGH] Fix blocking async calls in X — thread pool starvation risk
2. [MEDIUM] Switch to ArrayPool in Z — reduces GC pressure on upload path
```

**Impact levels:**
- **HIGH**: Measurable gain, prevents starvation, fixes correctness, reduces P95 latency. Examples: blocking async, missing CancellationToken, N+1 queries, captive dependencies.
- **MEDIUM**: Reduces allocations, GC pressure, or unnecessary work. Examples: ArrayPool, StringBuilder, FrozenDictionary.
- **LOW**: Minor improvements, cold-path optimizations. Examples: initial collection capacity, Count() vs Any().

**Scale-based severity escalation.** When the same anti-pattern appears across many instances, escalate:

- 1–10 instances → report at the pattern's base severity
- 11–50 instances → escalate LOW patterns to MEDIUM
- 50+ instances → MEDIUM with elevated priority; flag as a codebase-wide systematic issue

Always report **exact counts from scan recipes**, not estimates. Group findings by severity (HIGH → MEDIUM → LOW), not by file. Merge related findings that share the same fix (e.g., all `.ToLower()` calls in one finding, not split per file).

### Phase 4: Optimization (Apply Fixes)

After presenting the report:
- If `--fix` in `$ARGUMENTS`, proceed directly.
- Otherwise ask: "Would you like me to apply these optimizations? I'll work one category at a time, starting with HIGH impact. You can specify categories or findings (e.g., 'fix ASYNC and MEM' or 'fix #1, #3')."

**Before any fix:**
1. **Read actual code context** around the grep match — false positives exist (`.Result` in `Task.FromResult` is NOT blocking).
2. Confirm the finding is real. If uncertain, flag as "needs manual review."

**Applying fixes:**
1. One category at a time, highest impact first. Use Edit tool with brief before/after summary.
2. After each category: `dotnet build --no-restore`
3. After all changes: `dotnet test`
4. If build or tests fail, diagnose before continuing.

---

## Analyzer Radar

- `CA1826`, `CA1827`, `CA1829`, `CA1836`, `CA1851`, `CA1860` — LINQ and enumeration
- `CA1845`, `CA1846`, `CA1858` — string and span-friendly APIs
- `CA1834`, `CA1865`–`CA1867` — StringBuilder char overloads
- `CA1870` — cached `SearchValues<T>`

These are clues, not goals. Apply where measured hot paths justify it.

## Pattern Guardrails

- Do not say "put it on the stack" as a blanket goal. Explain lifetime, copies, boxing, and escape rules.
- Do not suggest `stackalloc` for unbounded sizes, large buffers, or loop-carried allocations.
- Do not recommend `Span<T>` for data that crosses `await`, escapes to the heap, or lives in object fields — use `Memory<T>`.
- Do not recommend converting every `class` to a `struct` — large, mutable, or frequently boxed types often get worse.
- Do not blanket-rewrite LINQ to loops — use analyzer-backed fixes first.
- Do not recommend pooling without ownership rules — returned pooled arrays must not be reused by the caller.
- Do not recommend `GC.Collect()` except for rare justified lifecycle boundaries with measurement.

## Red Flags — STOP and Confirm

Stop and ask before:
- Changing `Program.cs` or the middleware pipeline
- Adding a new NuGet package
- Changing any DI service lifetime registration
- Replacing the serializer in the HTTP pipeline
- Modifying API response shapes or route patterns
- Changing error handling patterns

## Constraints

- NEVER add NuGet packages without user approval
- NEVER change DI lifetimes without explaining implications and getting confirmation
- NEVER modify `Program.cs` or middleware pipeline without explicit approval
- NEVER change API contracts, route patterns, or response shapes
- ALWAYS preserve existing tests; update only if behavior intentionally changes
- ALWAYS use the Grep tool for searches, never bash `grep` or `find`

Consult the project's CLAUDE.md or AGENTS.md for project-specific rules and constraints.
