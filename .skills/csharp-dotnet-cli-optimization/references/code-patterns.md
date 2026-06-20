---
description: Code-level performance patterns for csharp-dotnet-cli-optimization.
metadata:
  tags: [linq, span, stackalloc, boxing, pooling, strings, analyzers]
---

# Code Patterns

Use this reference after a profile or benchmark identifies a hot path. Do not apply these patterns speculatively.

## Table Of Contents

- [LINQ And Enumeration](#linq-and-enumeration)
- [Stack Allocation, Span, And Memory](#stack-allocation-span-and-memory)
- [Structs, Boxing, And Copies](#structs-boxing-and-copies)
- [Buffer Reuse And Advanced Helpers](#buffer-reuse-and-advanced-helpers)
- [Strings](#strings)
- [What Not To Suggest](#what-not-to-suggest)

## LINQ And Enumeration

### Property or indexer over LINQ when the concrete collection is known

Wrong:

```csharp
if (items.Count() > 0)
{
    return items.First();
}
```

Better:

```csharp
if (items.Count > 0)
{
    return items[0];
}
```

Use `Count`, `Length`, `IsEmpty`, or an indexer when you already have a concrete collection with that API. Relevant analyzers: `CA1826`, `CA1829`, `CA1836`, `CA1860`.

### `Any()` over `Count() > 0` when all you know is `IEnumerable<T>`

Wrong:

```csharp
if (source.Count() != 0)
{
    Process(source);
}
```

Better:

```csharp
if (source.Any())
{
    Process(source);
}
```

Relevant analyzer: `CA1827`.

### Avoid multiple enumeration of deferred queries

Wrong:

```csharp
var query = source.Where(Filter);
return query.Count() + query.Last().Id;
```

Better:

```csharp
var materialized = source.Where(Filter).ToArray();
return materialized.Length + materialized[^1].Id;
```

Materialize once only when you truly need multiple passes or random access and can afford the extra memory. Relevant analyzer: `CA1851`.

### Avoid premature materialization

Wrong:

```csharp
var projected = source.ToList().Select(Map);
```

Better:

```csharp
var projected = source.Select(Map);
```

Keep deferred execution unless you need a snapshot, repeated traversal, indexing, or a boundary between expensive stages.

### Do not blanket-rewrite LINQ to loops

- .NET 10 improved many LINQ operations substantially.
- Start with analyzer-backed fixes and measurement.
- Replace LINQ with hand-written loops only when a benchmark or trace shows that the remaining cost matters.

### Use `TryGetNonEnumeratedCount` when count is optional

```csharp
if (source.TryGetNonEnumeratedCount(out int count))
{
    LogCount(count);
}
```

This avoids forcing enumeration when the underlying type already knows its size.

## Stack Allocation, Span, And Memory

### `stackalloc` only for small, bounded, temporary buffers

Wrong:

```csharp
for (int i = 0; i < items.Length; i++)
{
    Span<byte> buffer = stackalloc byte[4096];
    Use(buffer);
}
```

Better:

```csharp
Span<byte> buffer = stackalloc byte[256];
for (int i = 0; i < items.Length; i++)
{
    buffer.Clear();
    Use(buffer);
}
```

Guidance:

- keep sizes conservative
- avoid `stackalloc` inside loops
- initialize the memory before use
- fall back to heap or pooling for larger or variable-sized buffers

### Prefer span-based APIs over substring copies

Wrong:

```csharp
int.TryParse(line.Substring(7), out int value);
```

Better:

```csharp
int.TryParse(line.AsSpan(7), out int value);
```

Relevant analyzers: `CA1845`, `CA1846`.

### Use `Span<T>` for sync work and `Memory<T>` for async or heap-stored state

Wrong:

```csharp
// Wrong: Span<T> cannot cross await safely.
public async Task<int> ReadAsync(Span<byte> buffer)
{
    await socket.ReceiveAsync(buffer);
    return buffer[0];
}
```

Better:

```csharp
public async Task<int> ReadAsync(Memory<byte> buffer)
{
    await socket.ReceiveAsync(buffer);
    return buffer.Span[0];
}
```

`Span<T>` is stack-only. If the lifetime crosses `await`, callbacks, or object storage, move to `Memory<T>`.

### `ref struct` is for stack-bound wrappers, not a general optimization badge

- Use `ref struct` when the type itself contains spans or must never escape to the heap.
- Do not use it if you need arrays of that type, boxing, interface conversions, or heap fields.

## Structs, Boxing, And Copies

### Use `readonly struct` or `readonly record struct` for small immutable values

Wrong:

```csharp
public struct Measurement
{
    public double A;
    public double B;
    public void Normalize() => A /= B;
}
```

Better:

```csharp
public readonly record struct Measurement(double A, double B);
```

Prefer value types for small, copyable, data-only values. Avoid large, mutable structs.

### Pass large structs by `in`

Wrong:

```csharp
double Distance(Vector4 value) => value.X + value.Y + value.Z + value.W;
```

Better:

```csharp
double Distance(in Vector4 value) => value.X + value.Y + value.Z + value.W;
```

This avoids copying large struct values on each call.

### Avoid boxing in hot paths

Wrong:

```csharp
object boxed = valueStruct;
```

Wrong:

```csharp
IFormattable f = valueStruct;
```

Better:

```csharp
Use(in valueStruct);
```

Boxing allocates a heap object and copies the value. Interface conversions can box too.

### Mark readonly members on structs

- Non-readonly instance members on a readonly receiver can trigger defensive copies.
- Mark the whole struct `readonly` when possible, or mark readonly members explicitly.

## Buffer Reuse And Advanced Helpers

### Use `ArrayPool<T>` when the buffer is too large or variable for `stackalloc`

Wrong:

```csharp
byte[] temp = new byte[inputLength];
```

Better:

```csharp
byte[] temp = ArrayPool<byte>.Shared.Rent(inputLength);
try
{
    Use(temp);
}
finally
{
    ArrayPool<byte>.Shared.Return(temp);
}
```

Rules:

- return to the same pool once
- never use the buffer after return
- rented arrays may be larger than requested
- rented arrays are not guaranteed to be zeroed

### Prevent accidental closure capture

Wrong:

```csharp
return values.Select(v => v * 2).ToArray();
```

Better when no capture is needed:

```csharp
return values.Select(static v => v * 2).ToArray();
```

Use `static` lambdas or static local functions to prevent capture when the delegate does not need outer state.

### Cache `SearchValues<T>` for repeated searches

Wrong:

```csharp
int index = text.IndexOfAny(":/?&=".AsSpan());
```

Better:

```csharp
private static readonly SearchValues<char> s_delims =
    SearchValues.Create(":/?&=".AsSpan());
```

```csharp
int index = text.IndexOfAny(s_delims);
```

Relevant analyzer: `CA1870`.

### `CollectionsMarshal.AsSpan` is advanced and ownership-sensitive

```csharp
Span<int> span = CollectionsMarshal.AsSpan(list);
```

Use this only when:

- you own the `List<T>`
- you will not add or remove items while the span is in use
- a measured hot path justifies bypassing normal list APIs

## Strings

### `StartsWith` over `IndexOf(...) == 0`

Wrong:

```csharp
return text.IndexOf("abc", StringComparison.Ordinal) == 0;
```

Better:

```csharp
return text.StartsWith("abc", StringComparison.Ordinal);
```

Relevant analyzer: `CA1858`.

### `Append(char)` over `Append("x")`

Wrong:

```csharp
builder.Append("]");
```

Better:

```csharp
builder.Append(']');
```

Relevant analyzers: `CA1834`, `CA1865-CA1867`.

## What Not To Suggest

- Do not suggest unsafe code first.
- Do not suggest pooling tiny objects by default.
- Do not suggest `stackalloc` because "heap bad, stack good".
- Do not suggest converting APIs to `Span<T>` if the lifetime model does not fit.
- Do not suggest loop rewrites without a profile or benchmark showing LINQ still matters after simpler fixes.
