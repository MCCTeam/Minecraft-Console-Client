---
description: CLR memory model and GC guidance for csharp-dotnet-cli-optimization.
metadata:
  tags: [clr, gc, heap, stack, loh, memory-model]
---

# CLR Memory Model And GC

Use this reference when the user asks why allocations, boxing, heap growth, GC pauses, or stack-based techniques behave the way they do.

## Core Model

- Reference types allocate objects on the managed heap. Local variables and fields hold references to those objects.
- Value types store their data directly. A value type local is often stored in stack storage, but value types also live inline inside object fields and array elements.
- "`struct` means stack" is false. The useful distinction is inline storage plus copy semantics, not "stack forever".
- Boxing converts a value type to `object` or an interface by allocating a new heap object and copying the value into it.
- `ref struct` types, including `Span<T>` and `ReadOnlySpan<T>`, are stack-constrained wrappers that can't escape to the managed heap.
- `Memory<T>` and `ReadOnlyMemory<T>` are the heap-storable counterparts when data must live across `await`, callbacks, or object fields.

## How The GC Works

- The GC is generational: gen0 for young objects, gen1 as a buffer, gen2 for long-lived survivors.
- The large object heap (LOH) is used for allocations at or above 85,000 bytes by default.
- Background GC is enabled by default. It reduces pause impact for full collections but does not make them free.
- Server GC and workstation GC are process-level choices. The defaults are usually right unless measurement says otherwise.
- On modern 64-bit Windows and Linux, the GC internally uses regions, but the optimization model for application code is still about generations, allocation rate, survivor rate, LOH churn, and pinning.

## What Usually Makes GC Expensive

- High allocation rate on hot paths
- Objects surviving long enough to promote into older generations
- Large transient allocations that churn the LOH
- Excessive pinning that increases fragmentation
- Finalizers on objects that should have been deterministic `Dispose` calls instead

## Wrong vs Better

| Wrong | Better | Why |
|---|---|---|
| Assume a `struct` is always stack allocated | Explain whether it will be copied, boxed, stored inline, or escape | That is what actually drives cost |
| Allocate large temporary arrays repeatedly | Reuse, pool, or redesign the algorithm if measurement shows LOH churn | LOH allocations are cleared and collected with gen2 work |
| Call `GC.Collect()` to "fix" memory pressure | Lower allocation rate and object lifetime first | Forced GC usually adds pause time and hides the real problem |
| Pin many buffers for long periods | Minimize pin count and pin duration | Pinning can fragment the heap |
| Use finalizers for routine cleanup | Use `IDisposable`, `using`, and `SafeHandle` for unmanaged resources | Finalization is slower and delays reclamation |

## GC Configuration Rules

- Treat GC configuration changes as process-wide tuning, not local fixes.
- Prefer runtime defaults unless counters and traces show a clear reason to change them.
- Choose server GC for throughput-oriented workloads only after measurement.
- Use low-latency modes sparingly and for bounded windows. They reduce GC intrusiveness by letting memory grow and can increase fragmentation.
- If you are tuning in containers or hard memory limits, treat heap hard-limit settings as operational controls, not code-level optimizations.

## Bad vs Good Examples

Bad:

```csharp
for (int i = 0; i < 10_000; i++)
{
    DoWork(new byte[200_000]);
}
GC.Collect();
```

Better:

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(200_000);
try
{
    for (int i = 0; i < 10_000; i++)
    {
        DoWork(buffer);
    }
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

Bad:

```csharp
public sealed class NativeThing
{
    ~NativeThing() => ReleaseHandle();
}
```

Better:

```csharp
public sealed class NativeThing : IDisposable
{
    public void Dispose()
    {
        ReleaseHandle();
        GC.SuppressFinalize(this);
    }
}
```

## Practical Heuristics

- If counters show rising allocation rate and frequent gen0 collections, start by eliminating short-lived allocations.
- If gen2 collections or LOH size are the problem, look for survivor growth, pinned buffers, and large transient objects.
- If a change turns classes into structs, verify both allocation wins and copy costs.
- If the process is memory-constrained, inspect runtime GC settings before changing code blindly.

## What Not To Claim

- Do not claim that moving code to `struct` always reduces memory.
- Do not claim that stack allocation is always faster than pooling.
- Do not claim that background GC removes pause concerns.
- Do not claim that the GC is the problem unless counters, traces, or dumps support that diagnosis.
