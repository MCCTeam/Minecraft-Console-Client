---
description: Dated source ledger for csharp-dotnet-cli-optimization.
metadata:
  tags: [sources, diagnostics, gc, linq, span, stackalloc, boxing]
---

# Sources

## Current primary sources

- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters), Microsoft Learn, updated `2025-10-02`
  - Canonical CLI docs for live counters, `monitor`, `collect`, and `dnx` one-shot execution on .NET 10.0.100+.
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace), Microsoft Learn, updated `2026-03-20`
  - Canonical CLI docs for `collect`, `report`, and the preview `collect-linux` path plus its limits.
- [dotnet-dump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump), Microsoft Learn, updated `2026-03-04`
  - Canonical CLI docs for dump collection, dump types, and `analyze -c`.
- [dotnet-gcdump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump), Microsoft Learn, updated `2025-12-17`
  - Canonical CLI docs for GC dump collection, `report`, and the induced full Gen 2 GC caveat.
- [Fundamentals of garbage collection](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals), Microsoft Learn, updated `2025-10-22`
  - Current official overview of generations, allocation, and managed heap behavior.
- [Runtime configuration options for garbage collection](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector), Microsoft Learn, updated `2025-11-22`
  - Current official source for server vs workstation GC, background GC, heap limits, LOH threshold, and modern GC configuration behavior.
- [stackalloc expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc), Microsoft Learn, updated `2026-01-24`
  - Current official guidance for stack allocation limits, loop avoidance, initialization, and Span-based usage.
- [ref struct types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct), Microsoft Learn, updated `2026-01-20`
  - Current official guidance for stack-only semantics and escape restrictions.
- [Structure types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct), Microsoft Learn, updated `2026-01-14`
  - Current official source for readonly structs, pass-by-reference guidance, and boxing conversions.
- [Value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-types), Microsoft Learn, updated `2026-01-20`
  - Current official source for copy semantics and inline storage behavior.
- [Boxing and Unboxing](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing), Microsoft Learn, updated `2025-10-13`
  - Current official source for boxing semantics and cost.
- [Memory<T> and Span<T> usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines), Microsoft Learn, updated `2025-04-11`
  - Current official guidance for choosing `Span<T>` vs `Memory<T>` and ownership rules.
- [Lambda expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions), Microsoft Learn, updated `2026-01-24`
  - Current official source for capture semantics and `static` lambdas.
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14), Microsoft Learn, updated `2025-11-19`
  - Current official confirmation of first-class span conversions in C# 14.
- [Performance rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/performance-warnings), Microsoft Learn, updated `2025-10-29`
  - Current official index of analyzer-backed performance rules, including `CA1870`.
- [Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/), Stephen Toub, published `2025-09-10`
  - High-trust expert source showing real .NET 10 runtime and LINQ improvements. Use it to avoid stale folklore such as "all LINQ is slow".

## Specific analyzer pages used for code-pattern guidance

- [CA1827](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1827), updated `2023-11-14`
- [CA1845](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1845), updated `2024-11-12`
- [CA1846](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1846), updated `2023-12-16`
- [CA1851](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1851), updated `2023-11-14`
- [CA1858](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1858), current official analyzer page
- [CA1860](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1860), current official analyzer page

## Older but still canonical sources used cautiously

- [Reduce memory allocations using new C# features](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/), Microsoft Learn, updated `2023-10-17`
  - Still useful for `ref`, `in`, readonly struct, and copy-avoidance guidance, but older than the core 2025-2026 docs.
- [Intermediate materialization](https://learn.microsoft.com/en-us/dotnet/standard/linq/intermediate-materialization), Microsoft Learn, updated `2022-09-02`
  - Still canonical for LINQ materialization semantics.
- [Deferred execution and lazy evaluation](https://learn.microsoft.com/en-us/dotnet/standard/linq/deferred-execution-lazy-evaluation), Microsoft Learn, updated `2022-09-29`
  - Still canonical for LINQ deferred-execution semantics.
- [dotnet-stack](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-stack), Microsoft Learn, updated `2023-03-14`
  - Used only for narrow stack-snapshot guidance because the page is stale compared to the other diagnostics docs.

## Explicit exclusions

- Framework-specific tutorials were intentionally excluded.
- GUI-first analysis flows were intentionally excluded.
- MCC-specific paths, code, and hot-path examples were intentionally excluded.
