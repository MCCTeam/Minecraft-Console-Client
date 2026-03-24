---
name: csharp-optimization
description: >-
  Use when optimizing C# code in MCC, reducing GC pressure, profiling hot paths,
  fixing latency spikes, or reviewing code for allocation or throughput issues.
metadata:
  category: technique
  triggers: performance, allocations, GC, hot path, latency, throughput,
    memory pressure, optimize, slow, freeze, lag spike, packet processing speed
version: 0.2.0
---

# C# Performance Optimization for MCC

Hands-on optimization recipes for Minecraft Console Client hot paths.
Complements `csharp-best-practices` (conventions) with measurement-driven
performance work.

## When to Use

- Profiling or reducing GC pressure in a running MCC session
- Optimizing per-packet code (`Protocol18.HandlePacket`, `DataTypes.ReadNext*`)
- Optimizing per-tick code (`PlayerPhysics.Tick`, `CollisionDetector.Collide`)
- Speeding up chunk decoding (`Protocol18Terrain.ProcessChunkColumnData`)
- Improving A* pathfinding (`Movement.CalculatePath`)
- Reviewing any code change for allocation or throughput regressions

**NOT for:**
- Login, config parsing, or one-shot command handlers (prefer clarity there)
- Style/convention questions (use `csharp-best-practices` instead)

---

## Iron Rule: Measure First

**NEVER optimize without profiling data.**

Guessing which code is slow is wrong more often than right. Measure, change,
re-measure. If you cannot show a before/after number, the optimization is not
justified.

| Rationalization | Reality |
|-----------------|---------|
| "This is obviously slow" | Obvious to you is not obvious to the JIT. Measure. |
| "I'll profile later" | Later never comes. Profile now or don't optimize. |
| "It's just one allocation" | On a 20 TPS tick, one allocation = 20 per second = GC pressure. Measure. |
| "AggressiveInlining everywhere" | The JIT already inlines small methods. Prove it helps before adding. |

---

## MCC Hot-Path Map

Know which code runs at which frequency before deciding where to invest:

| Frequency | Key paths (actual files) | Priority |
|---|---|---|
| Per-packet (100s/sec) | `Protocol/Handlers/Protocol18.cs` HandlePacket, `Protocol/Handlers/DataTypes.cs` ReadNext* | **High** |
| Per-tick (20/sec) | `Physics/PlayerPhysics.cs` Tick, `Physics/CollisionDetector.cs` Collide, ChatBot `Update()` | **High** |
| Per-chunk-load | `Protocol/Handlers/Protocol18Terrain.cs` ProcessChunkColumnData, ReadBlockStatesField | Medium |
| Per-pathfind | `Mapping/Movement.cs` CalculatePath (A*) | Medium |
| Per-connection | Login, registry sync, config | Low |
| Per-user-action | Commands, chat | Low |

---

## Profiling Recipes

### 1. Live GC monitoring

```bash
dotnet-counters ps                     # find MinecraftClient PID
dotnet-counters monitor --process-id <PID> \
  --counters System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate]
```

Healthy idle MCC: near-zero Gen-1/Gen-2 collections. Frequent Gen-0 during idle
means a hot-path allocation needs attention.

### 2. Allocation tracking

```bash
dotnet-trace collect --process-id <PID> \
  --providers Microsoft-Windows-DotNETRuntime:0x1:5
```

Open `.nettrace` in PerfView to find top-allocated types and call stacks.

### 3. Isolated benchmarks (BenchmarkDotNet)

Extract the hot method, add `[MemoryDiagnoser]`. Key columns: **Mean**,
**Allocated**, **Gen0**.

---

## Allocation Reduction (Highest Impact)

Reducing GC pressure directly reduces latency spikes in a long-running client.

### Pattern: Reuse per-tick buffers

```csharp
// BEFORE: new List every tick (20 allocations/sec)
var result = new List<Aabb>();

// AFTER: thread-local reuse (0 allocations/sec)
[ThreadStatic] private static List<Aabb>? t_buf;
var result = t_buf ??= new List<Aabb>(64);
result.Clear();
```

`[ThreadStatic]` works when single-threaded and non-reentrant (physics tick).
If reentrant: use `ObjectPool<T>`. If cross-thread: use `ArrayPool<T>`.

### Pattern: stackalloc for small fixed buffers

MCC already does this in `DataTypes.cs` for endian-swapped reads:

```csharp
Span<byte> rawValue = stackalloc byte[8];
for (int i = 7; i >= 0; --i) rawValue[i] = cache.Dequeue();
return BitConverter.ToDouble(rawValue);
```

Rules: under 512 bytes, known size at compile time, never inside loops or recursion.

### Pattern: Span slicing instead of array copies

```csharp
// BEFORE: allocates
byte[] sub = new byte[length];
Array.Copy(source, offset, sub, 0, length);

// AFTER: zero-copy
ReadOnlySpan<byte> sub = source.AsSpan(offset, length);
```

Critical in packet parsing where many fields are sliced from one buffer.

---

## Hot-Path Tuning

### MethodImpl attributes

MCC uses `[MethodImpl]` on its hottest paths. Match the attribute to the method:

| Attribute | When | MCC examples |
|---|---|---|
| `AggressiveInlining` | Tiny methods (< ~32 bytes IL), called millions of times | `Vec3d.Add`, `Aabb.Intersects`, `Chunk.SetWithoutCheck` |
| `AggressiveOptimization` | Larger critical-path methods | `ReadBlockStatesField`, `ProcessChunkColumnData` |
| Both | Medium methods, very high frequency | `DataTypes.ReadNextVarInt`, `ReadDataReverse` |
| Neither | Infrequent code | Login, config, commands |

**Do not scatter `AggressiveInlining` without profiling evidence.** The JIT
already inlines small methods.

### BinaryPrimitives over BitConverter

```csharp
// BEFORE: manual endian swap
(buf[0], buf[3]) = (buf[3], buf[0]);
int val = BitConverter.ToInt32(buf);

// AFTER: direct big-endian read, no branch
int val = BinaryPrimitives.ReadInt32BigEndian(buf);
```

### MemoryMarshal for bulk reads

Already used in chunk decoding for zero-copy packed-long reads:
```csharp
ReadOnlySpan<long> longs = MemoryMarshal.Cast<byte, long>(entryData);
```

---

## Data Structure Selection

### Frozen collections for palettes

Palette maps are built once and read millions of times. `FrozenDictionary`
gives ~50% faster reads than `Dictionary`:

```csharp
private static readonly FrozenDictionary<int, Material> s_palette =
    new Dictionary<int, Material> { ... }.ToFrozenDictionary();
```

Apply to: `BlockPalettes/*.cs`, `EntityPalettes/*.cs`, `ItemPalettes/*.cs`,
`PacketPalettes/*.cs`, any `static readonly Dictionary` populated once.

### PriorityQueue for A*

`Movement.cs` has a custom `BinaryHeap`. The built-in `PriorityQueue<TElement,
TPriority>` (.NET 6+) is well-optimized and avoids maintenance burden.

### ConcurrentDictionary sizing

Pre-size `World.chunks` to avoid rehashing:
```csharp
new ConcurrentDictionary<(int, int), ChunkColumn>(
    concurrencyLevel: Environment.ProcessorCount, capacity: 1024);
```

---

## Threading

### Minimize lock scope

Copy data out under the lock, process outside:
```csharp
List<Item> snapshot;
lock (_lock) { snapshot = [.. _items]; }
foreach (var item in snapshot) ExpensiveProcess(item);
```

### Batch InvokeOnMainThread

Each `InvokeOnMainThread()` call blocks until the main thread runs it.
In loops, batch into a single call:
```csharp
handler.InvokeOnMainThread(() =>
{
    foreach (var entity in entities) UpdateEntity(entity);
});
```

### Channel\<T\> over BlockingCollection\<T\>

Lower overhead, async-friendly:
```csharp
var ch = Channel.CreateUnbounded<(int Id, Memory<byte> Data)>(
    new UnboundedChannelOptions { SingleReader = true });
```

---

## Common Optimization Anti-Patterns

These are things agents (and humans) rationalize doing. Every one of them
makes performance worse or wastes effort.

| Anti-pattern | Why it's wrong |
|---|---|
| Adding `AggressiveInlining` to large methods | Bloats call sites, causes more cache misses, makes code *slower* |
| Optimizing login/config code | Runs once per session; clarity matters more than speed |
| Using `ConcurrentDictionary` where a plain `Dictionary` + lock suffices | Concurrent overhead on uncontested paths costs more than a lock |
| Replacing LINQ with manual loops on cold paths | No measurable gain, worse readability |
| Caching mutable state to avoid re-reads | Stale cache bugs are harder to diagnose than the perf hit |
| `Task.Result` / `.Wait()` on hot paths | Deadlock risk and thread-pool starvation |

---

## Pre-Commit Checklist

ALWAYS verify before submitting a performance change:

- [ ] Hot path identified with profiling data, not guesswork
- [ ] Before/after measurements recorded (allocation count, throughput, or latency)
- [ ] No new allocations inside per-tick or per-packet methods
- [ ] `[MethodImpl]` attributes match method call frequency and IL size
- [ ] Frozen collections used for any static lookup table
- [ ] Lock scopes contain no I/O or expensive work
- [ ] No `Task.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on hot paths
- [ ] Thread safety preserved (checked existing lock/concurrent patterns)
- [ ] Optimization comments explain non-obvious choices
- [ ] Code still compiles and passes all existing checks
