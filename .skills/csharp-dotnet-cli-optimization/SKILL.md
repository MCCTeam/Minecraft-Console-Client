---
name: csharp-dotnet-cli-optimization
description: >-
  Use when diagnosing or optimizing generic C#/.NET performance, GC pressure,
  allocations, heap or stack usage, LINQ overhead, boxing, Span/Memory,
  stackalloc, pooling, or hot-path code with CLI-first tools such as
  dotnet-counters, dotnet-trace, dotnet-stack, dotnet-gcdump, dotnet-dump, or
  BenchmarkDotNet.
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
    - memory
    - boxing
    - heap
    - stack
    - latency
    - throughput
    - slow
    - hang
    - deadlock
version: 0.2.0
---

# C#/.NET CLI Optimization

CLI-first guidance for generic C# 14 / .NET 10 performance work.
Use the references on demand:

- Read [references/memory-model-gc.md](references/memory-model-gc.md) for stack vs heap, generations, LOH, pinning, server vs workstation GC, and GC tuning limits.
- Read [references/code-patterns.md](references/code-patterns.md) for LINQ, Span/Memory, stackalloc, structs, boxing, pooling, strings, and analyzer-backed code patterns.
- Read [references/README.md](references/README.md) for dated sources and freshness notes.

## When to Use

- A .NET process is slow, allocation-heavy, CPU-heavy, or memory-hungry
- A live process appears stuck, hung, or deadlocked
- The user asks how heap, stack, GC, boxing, or LINQ overhead actually works in .NET
- The user wants concrete bad vs good code patterns after measurement has identified a hot path
- The task needs a decision between counters, traces, stacks, GC dumps, dumps, or a benchmark

**NOT for:**
- ASP.NET Core, EF Core, MAUI, Orleans, Unity, Avalonia, WPF, WinForms, Blazor, or other framework-specific playbooks
- Visual Studio, Rider, VS Code, PerfView, speedscope, or any GUI-first workflow
- speculative rewrites such as "replace everything with Span" before measurement

## Iron Rule

ALWAYS measure first, change second, and re-measure third.

NEVER claim an optimization without before/after evidence from the same scenario.

| Rationalization | Reality |
|---|---|
| "This is obviously slow" | The runtime, JIT, and libraries often invalidate intuition. |
| "struct means stack" | Value types are stored inline. They are not "always on the stack". |
| "All LINQ is slow" | .NET 10 improved many LINQ paths. Measure before rewriting. |
| "GC.Collect will fix it" | Forced collection usually treats symptoms, not cause. |

## Investigation Order

1. Use `dotnet-counters` for live triage.
2. If the process is stuck, capture `dotnet-stack` immediately.
3. If CPU or allocation hot paths matter, collect `dotnet-trace`.
4. If heap growth matters more than call paths, collect `dotnet-gcdump`.
5. If you need SOS heap inspection or a postmortem, collect `dotnet-dump`.
6. Only after live evidence points to a candidate routine, apply patterns from the reference docs.
7. If the change is truly local and isolated, use BenchmarkDotNet to compare implementations.
8. Re-run the original live capture to prove the real workload improved.

## Which Reference to Load

| User question | Read first |
|---|---|
| "How do stack and heap really work in .NET?" | `references/memory-model-gc.md` |
| "Why is GC pausing or why is LOH churn hurting us?" | `references/memory-model-gc.md` |
| "How should I optimize this LINQ?" | `references/code-patterns.md` |
| "Can I move this to the stack with stackalloc or Span?" | `references/code-patterns.md` |
| "Should this be a struct, ref struct, readonly struct, or class?" | `references/code-patterns.md` and `references/memory-model-gc.md` |
| "Why is this boxing?" | `references/code-patterns.md` |

## Tool Selection

| Question | Tool | What it answers | Typical next step |
|---|---|---|---|
| Is the live process allocating, GCing, or saturating CPU? | `dotnet-counters` | Live counters and trend direction | Capture a trace or GC dump if suspicious |
| Is the process hung or deadlocked right now? | `dotnet-stack` | Current managed stack snapshot | Collect a dump if you need deeper postmortem evidence |
| Which call paths consume CPU or allocate heavily? | `dotnet-trace` | Sampled execution and runtime events | Confirm hot paths, then isolate code |
| Which object types dominate managed heap usage? | `dotnet-gcdump` | Heap composition and type totals | Decide whether to redesign lifetimes or collect a full dump |
| Do I need SOS heap inspection or thread state? | `dotnet-dump` | Full dump plus CLI analysis | Run `analyze -c` commands |
| Did a code change improve one isolated routine? | BenchmarkDotNet | Reproducible microbenchmark comparison | Re-run live diagnostics in the real scenario |

## Pattern Guardrails

- Do not answer "put it on the stack" as a blanket goal. Explain lifetime, copies, boxing, and escape rules instead.
- Do not suggest `stackalloc` for unbounded sizes, large buffers, or loop-carried allocations.
- Do not recommend `Span<T>` for data that must cross `await`, escape to the heap, or live in object fields. Switch to `Memory<T>` or `ReadOnlyMemory<T>` for that.
- Do not recommend converting every `class` to a `struct`. Large, mutable, identity-bearing, or frequently boxed types often get worse.
- Do not blanket-rewrite LINQ to loops. Use analyzer-backed fixes first, and remember .NET 10 substantially improved many LINQ paths.
- Do not recommend pooling without ownership rules. Returned pooled arrays must not be reused by the caller.
- Do not recommend `GC.Collect()` except for rare, justified lifecycle boundaries, and only with measurement.

## Analyzer Radar

When performance diagnostics point to code patterns rather than runtime configuration, consult the current performance analyzers, especially:

- `CA1826`, `CA1827`, `CA1829`, `CA1836`, `CA1851`, `CA1860` for LINQ and enumeration
- `CA1845`, `CA1846`, `CA1858` for string and span-friendly APIs
- `CA1834`, `CA1865-CA1867` for `StringBuilder` char overloads
- `CA1870` for cached `SearchValues<T>`

These rules are clues, not goals. Apply them where the measured hot path justifies it.

## Minimal Commands

```bash
dnx dotnet-counters monitor --process-id <PID>
dotnet-counters monitor -p <PID> --counters System.Runtime
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
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
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

## Output

When using this skill, report:

- the measured symptom and the evidence used to identify it
- the chosen tool or code pattern and why it fits this bottleneck
- the relevant tradeoff, such as allocation vs copy cost, deferred vs eager execution, or stack vs pool
- the before/after result, or say explicitly if the recommendation is still unverified
