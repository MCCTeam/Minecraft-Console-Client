---
name: csharp-optimization
description: >
  C# performance optimization for the Minecraft Console Client codebase. Covers profiling,
  allocation reduction, hot-path tuning, Span/pooling patterns, threading, and data structure
  selection. Use this whenever the user wants to optimize C# code, reduce GC pressure,
  speed up packet processing, improve physics/pathfinding performance, profile MCC, or
  review code for performance issues. Also use when the user mentions "performance",
  "allocations", "GC", "hot path", "latency", "throughput", "memory pressure", or "optimize".
version: 0.1.0
---

# C# Performance Optimization for MCC

This skill complements `csharp-best-practices` with hands-on optimization workflows
specific to the Minecraft Console Client. Use `csharp-best-practices` for conventions
and idiomatic patterns; use this skill for measuring, profiling, and transforming code
to run faster or allocate less.

## When to optimize

Not every path needs optimization. Focus effort where it matters:

| Frequency | MCC examples | Optimization priority |
|---|---|---|
| Per-packet (hundreds/sec) | `Protocol18.HandlePacket`, `DataTypes.ReadNext*` | High |
| Per-tick (20/sec) | `PlayerPhysics.Tick`, `CollisionDetector.Collide`, bot `Update()` | High |
| Per-chunk-load | `Protocol18Terrain.ProcessChunkColumnData` | Medium |
| Per-pathfind | `Movement.CalculatePath` (A*) | Medium |
| Per-connection | Login, config, registry sync | Low |
| Per-user-action | Commands, chat sending | Low |

Rule of thumb: if a method runs more than 20 times per second, measure before and
after every change. If it runs once per user action, prefer clarity over micro-tuning.

## Profiling workflow

### Quick allocation check

Use `dotnet-counters` to watch GC and allocation rates in a running MCC session:

```bash
# In one terminal, run MCC
dotnet run --project MinecraftClient -c Release

# In another terminal, attach counters
dotnet-counters monitor --process-id $(pidof MinecraftClient) \
  --counters System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate]
```

A healthy MCC session should show very few Gen-1/Gen-2 collections. Frequent Gen-0
collections during idle (no chunk loading, no pathfinding) indicate a leak or hot-path
allocation that needs attention.

### Targeted profiling with BenchmarkDotNet

For isolated hot paths, extract the method into a benchmark:

```csharp
[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class VarIntBenchmark
{
    private readonly byte[] _data = [0xFF, 0xFF, 0x7F]; // 2097151

    [Benchmark]
    public int ParseVarInt()
    {
        int result = 0, shift = 0;
        foreach (byte b in _data)
        {
            result |= (b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0) break;
        }
        return result;
    }
}
```

Key columns to watch: **Mean**, **Allocated**, and **Gen0** (collections per 1000 ops).

### Object Allocation Tracking (OAT)

For deeper allocation analysis, use `dotnet-trace` with the GC allocation tick event:

```bash
dotnet-trace collect --process-id $(pidof MinecraftClient) \
  --providers Microsoft-Windows-DotNETRuntime:0x1:5
```

Open the resulting `.nettrace` in Visual Studio or PerfView to see which types are
allocated most frequently and from which call stacks.

## Allocation reduction recipes

These are the highest-impact optimizations in a long-running game client like MCC,
because reducing GC pressure directly reduces pause-induced latency spikes.

### Recipe 1: Replace per-call List with pooled or stack buffer

**Before** (allocates a new list every physics tick):
```csharp
// CollisionDetector.cs - called 20x/sec
public static List<Aabb> CollectBlockColliders(World world, Aabb search)
{
    var result = new List<Aabb>();  // GC pressure
    for (int x = floor(search.MinX); x <= ceil(search.MaxX); x++)
        for (int y = floor(search.MinY); y <= ceil(search.MaxY); y++)
            for (int z = floor(search.MinZ); z <= ceil(search.MaxZ); z++)
                result.AddRange(GetBlockShapes(world, x, y, z));
    return result;
}
```

**After** (reuse a thread-local or pooled buffer):
```csharp
[ThreadStatic] private static List<Aabb>? t_colliderBuffer;

public static List<Aabb> CollectBlockColliders(World world, Aabb search)
{
    var result = t_colliderBuffer ??= new List<Aabb>(64);
    result.Clear();  // reuse, no allocation
    for (int x = floor(search.MinX); x <= ceil(search.MaxX); x++)
        for (int y = floor(search.MinY); y <= ceil(search.MaxY); y++)
            for (int z = floor(search.MinZ); z <= ceil(search.MaxZ); z++)
                result.AddRange(GetBlockShapes(world, x, y, z));
    return result;
}
```

Use `[ThreadStatic]` when the buffer is only accessed from one thread (physics tick).
Use `ObjectPool<T>` or `ArrayPool<T>` when shared across threads.

### Recipe 2: stackalloc for small fixed-size buffers

The codebase already does this well in `DataTypes.cs`:

```csharp
// GOOD: stackalloc for endian-swapped reads (8 bytes)
[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
public double ReadNextDouble(Queue<byte> cache)
{
    Span<byte> rawValue = stackalloc byte[8];
    for (int i = 7; i >= 0; --i)
        rawValue[i] = cache.Dequeue();
    return BitConverter.ToDouble(rawValue);
}
```

Guidelines for stackalloc:
- Use for buffers under 512 bytes with a known size at compile time.
- Never stackalloc in a loop or recursive method (stack overflow risk).
- Combine with `MemoryMarshal.Cast<byte, T>` to avoid `BitConverter` overhead:

```csharp
Span<byte> raw = stackalloc byte[8];
FillFromNetwork(raw);
long value = MemoryMarshal.Read<long>(raw);  // no BitConverter overhead
if (BitConverter.IsLittleEndian)
    value = BinaryPrimitives.ReverseEndianness(value);
```

### Recipe 3: Span slicing instead of array copies

**Before** (allocates a new array):
```csharp
byte[] sub = new byte[length];
Array.Copy(source, offset, sub, 0, length);
ProcessData(sub);
```

**After** (zero-copy slice):
```csharp
ReadOnlySpan<byte> sub = source.AsSpan(offset, length);
ProcessData(sub);
```

This matters most in packet parsing where many fields are sliced from a single buffer.

### Recipe 4: Avoid boxing in generic/collection contexts

```csharp
// WRONG: boxes the int on every call
void Track(object value) => _log.Add(value);
Track(42);  // int boxed to object

// CORRECT: generic avoids boxing
void Track<T>(T value) => _log.Add(value.ToString());
Track(42);  // no boxing
```

Watch for boxing in:
- `Dictionary<SomeEnum, V>` (enum key causes boxing in older .NET; fixed in .NET 8+)
- `string.Format` with value-type args (use interpolation or `CompositeFormat`)
- Event args that wrap value types in object

## Hot-path tuning

### MethodImpl attributes

MCC uses `[MethodImpl]` attributes on its hottest paths. Follow this pattern:

| Attribute | When to use | MCC examples |
|---|---|---|
| `AggressiveInlining` | Small methods called millions of times (< ~32 bytes IL) | `Vec3d.Add`, `Aabb.Intersects`, `Chunk.SetWithoutCheck` |
| `AggressiveOptimization` | Larger methods on critical paths; tells JIT to spend more time optimizing | `ReadBlockStatesField`, `ProcessChunkColumnData`, `AesCfb8Stream` |
| Both | Medium methods called very frequently | `DataTypes.ReadNextVarInt`, `ReadDataReverse` |
| Neither | Code that runs infrequently | Login, config parsing, command handlers |

Do not sprinkle `AggressiveInlining` everywhere. The JIT already inlines small methods.
Use it only when profiling shows that a specific call site is not being inlined but should be.

### BinaryPrimitives over BitConverter

`BinaryPrimitives` works on spans, avoids endian checks at runtime, and is
more inlining-friendly:

```csharp
// Before: BitConverter + manual endian swap
Span<byte> buf = stackalloc byte[4];
FillBytes(buf);
if (BitConverter.IsLittleEndian) buf.Reverse();
int val = BitConverter.ToInt32(buf);

// After: BinaryPrimitives reads big-endian directly
Span<byte> buf = stackalloc byte[4];
FillBytes(buf);
int val = BinaryPrimitives.ReadInt32BigEndian(buf);
```

### MemoryMarshal for bulk reads

The chunk decoder already uses this pattern for reading packed long arrays:

```csharp
// Zero-copy cast from byte span to long span (Protocol18Terrain.cs)
ReadOnlySpan<long> entryDataLong = MemoryMarshal.Cast<byte, long>(entryData);
```

Use `MemoryMarshal.Cast` when you need to reinterpret a byte buffer as a typed span.
Ensure correct endianness on the data before casting.

## Data structure optimization

### Frozen collections for static lookup tables

Palette maps (block IDs to materials, entity IDs to types, item IDs to names) are
built once at startup and never mutated. These are ideal candidates for
`FrozenDictionary` and `FrozenSet`:

```csharp
// Before: regular Dictionary, slower reads
private static readonly Dictionary<int, Material> s_palette = new() { ... };

// After: FrozenDictionary, ~50% faster reads on hot lookup paths
private static readonly FrozenDictionary<int, Material> s_palette =
    new Dictionary<int, Material> { ... }.ToFrozenDictionary();
```

Apply to:
- `BlockPalettes/*.cs` (block state ID to Material)
- `EntityPalettes/*.cs` (entity type ID to EntityType)
- `ItemPalettes/*.cs` (item ID to ItemType)
- `PacketPalettes/*.cs` (packet ID to type enum)
- Any `static readonly Dictionary` that is populated once

### PriorityQueue for pathfinding

The A* implementation in `Movement.cs` uses a custom `BinaryHeap`. The built-in
`PriorityQueue<TElement, TPriority>` (available since .NET 6) is well-optimized
and avoids the maintenance burden:

```csharp
// Before: custom BinaryHeap
var openSet = new BinaryHeap();
openSet.Insert(startNode);

// After: built-in PriorityQueue
var openSet = new PriorityQueue<Location, int>();
openSet.Enqueue(start, 0);
```

### ConcurrentDictionary sizing

`World.chunks` uses `ConcurrentDictionary`. Set initial capacity when the expected
size is known to avoid rehashing:

```csharp
// If a typical render distance of 12 loads ~625 chunks:
var chunks = new ConcurrentDictionary<(int, int), ChunkColumn>(
    concurrencyLevel: Environment.ProcessorCount,
    capacity: 1024);
```

## Threading optimization

### Minimize lock scope

Keep critical sections as short as possible. Copy data out under the lock, then
process it outside:

```csharp
// WRONG: processing inside lock holds it too long
lock (_lock)
{
    foreach (var item in _items)
        ExpensiveProcess(item);
}

// CORRECT: copy under lock, process outside
List<Item> snapshot;
lock (_lock)
{
    snapshot = [.. _items];
}
foreach (var item in snapshot)
    ExpensiveProcess(item);
```

### InvokeOnMainThread awareness

MCC dispatches cross-thread work via `McClient.InvokeOnMainThread()`. Each call
enqueues a delegate and blocks the caller until the main thread executes it. In
tight loops, batch work into a single `InvokeOnMainThread` call:

```csharp
// WRONG: N cross-thread round trips
foreach (var entity in entities)
    handler.InvokeOnMainThread(() => UpdateEntity(entity));

// CORRECT: one cross-thread call for the whole batch
handler.InvokeOnMainThread(() =>
{
    foreach (var entity in entities)
        UpdateEntity(entity);
});
```

### Prefer Channel\<T\> over BlockingCollection\<T\>

`Channel<T>` has lower overhead and integrates cleanly with async/await:

```csharp
// Modern pattern for producer-consumer packet queues
var channel = Channel.CreateUnbounded<(int Id, Memory<byte> Data)>(
    new UnboundedChannelOptions { SingleReader = true });

// Producer (network thread)
await channel.Writer.WriteAsync((packetId, data), ct);

// Consumer (handler thread)
await foreach (var packet in channel.Reader.ReadAllAsync(ct))
    HandlePacket(packet.Id, packet.Data);
```

## Optimization checklist

Before submitting a performance-related change, verify:

- [ ] Identified the hot path with profiling data, not guesswork
- [ ] Measured before and after (allocation count, throughput, or latency)
- [ ] Used `Span<T>` / `ReadOnlySpan<T>` instead of `byte[]` where possible
- [ ] No new allocations inside per-tick or per-packet methods
- [ ] `[MethodImpl]` attributes match the method's call frequency and size
- [ ] Frozen collections used for static lookup tables
- [ ] Lock scopes are minimal; no I/O or expensive work inside locks
- [ ] No `Task.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on hot paths
- [ ] Changes do not break thread safety (check existing lock/concurrent patterns)
- [ ] Code remains readable; optimization comments explain non-obvious choices
