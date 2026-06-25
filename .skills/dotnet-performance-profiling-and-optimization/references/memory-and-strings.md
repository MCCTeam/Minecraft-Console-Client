# Memory & String Patterns

### Use ReadOnlySpan\<byte\> for Constant Byte Data
🟡 **DO** assign constant byte arrays to `ReadOnlySpan<byte>` | .NET 5+

❌
```csharp
byte[] data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
```
✅
```csharp
ReadOnlySpan<byte> data = [0x48, 0x65, 0x6C, 0x6C, 0x6F];
ReadOnlySpan<int> primes = [2, 3, 5, 7, 11, 13];
```

**Impact: ~100x faster access than static byte[] field, zero allocation.**

### Use stackalloc for Small Temporary Buffers
🟡 **DO** use `stackalloc` for small, fixed-size temporary buffers | .NET Core+

❌
```csharp
char[] buffer = new char[64];
guid.TryFormat(buffer, out int written);
```
✅
```csharp
Span<char> buffer = stackalloc char[64];
guid.TryFormat(buffer, out int written);
```

**Impact: Zero heap allocation, no GC pressure, instant alloc/dealloc.**

### Use Span.TryWrite for Allocation-Free Interpolation
🟡 **DO** use `MemoryExtensions.TryWrite` to format into `Span<char>` buffers | .NET 6+

❌
```csharp
string formatted = $"Date: {dt:R}";
destination.Write(formatted);
```
✅
```csharp
Span<char> buffer = stackalloc char[64];
buffer.TryWrite($"Date: {dt:R}", out int charsWritten);
```

**Impact: Zero heap allocation for formatting operations.**

### Use Span.Split() for Zero-Allocation Splitting
🟡 **DO** use `MemoryExtensions.Split` for allocation-free string splitting | **.NET 10 (or .NET 9) only — NOT available on .NET 8**

❌ (allocates `string[]` — the only built-in option on .NET 8)
```csharp
string[] parts = input.Split(',');
```
✅ .NET 10
```csharp
foreach (Range range in input.AsSpan().Split(','))
{
    ReadOnlySpan<char> segment = input.AsSpan(range);
}
```
✅ .NET 8 fallback — manual `IndexOf` loop on the span (no allocation)
```csharp
ReadOnlySpan<char> remaining = input.AsSpan();
while (!remaining.IsEmpty)
{
    int idx = remaining.IndexOf(',');
    ReadOnlySpan<char> segment = idx < 0 ? remaining : remaining[..idx];
    // ... use segment ...
    remaining = idx < 0 ? default : remaining[(idx + 1)..];
}
```

**Impact: 208 bytes → 0 bytes per split, 2x faster on .NET 10. The manual .NET 8 loop is also zero-allocation but more verbose.**

### Use UTF8 String Literals (u8 suffix)
🟡 **DO** use the `u8` suffix for compile-time UTF8 `ReadOnlySpan<byte>` | .NET 7+

❌
```csharp
byte[] header = Encoding.UTF8.GetBytes("Content-Type");
```
✅
```csharp
ReadOnlySpan<byte> header = "Content-Type"u8;
```

**Impact: 17ns → 0.006ns — eliminates runtime transcoding entirely.**

### Use ReadOnlySpan\<char\> Pattern Matching with switch
🟡 **DO** use `switch` on `ReadOnlySpan<char>` for allocation-free string matching | C# 11+

❌
```csharp
switch (attr.Value.Trim()) { case "preserve": /* ... */ break; }
```
✅
```csharp
switch (attr.Value.AsSpan().Trim())
{
    case "preserve": return Preserve;
    case "default": return Default;
}
```

**Impact: Eliminates string allocation from Trim() in switch-based dispatch.**

### Use params ReadOnlySpan\<T\> to Eliminate Array Allocations
🟡 **DO** add `params ReadOnlySpan<T>` overloads to library methods | **.NET 10 (or .NET 9) only — requires C# 13**

❌ (the only option on .NET 8 — accept the array allocation, or add explicit 1/2/3-argument overloads)
```csharp
public static void Log(params string[] messages) { /* ... */ }
Log("Starting", "Processing", "Done");
```
✅ .NET 10
```csharp
public static void Log(params ReadOnlySpan<string> messages) { /* ... */ }
Log("Starting", "Processing", "Done");
```
✅ .NET 8 fallback — keep `params string[]` and add fixed-arity overloads for the hot common cases
```csharp
public static void Log(string m) { /* ... */ }
public static void Log(string m1, string m2) { /* ... */ }
public static void Log(string m1, string m2, string m3) { /* ... */ }
public static void Log(params string[] messages) { /* fallback for 4+ args */ }
```

**Impact: Eliminates params array allocation on .NET 10. On .NET 8 fixed-arity overloads cover the hot 1–3 argument cases.**

### Avoid Chained String-Returning Operations
🟡 **AVOID** chains of 3+ string-returning method calls that each allocate intermediates | .NET Core+

**Pattern 1: Chained .Replace() calls**

❌
```csharp
string result = input.Replace("a", "b").Replace("c", "d").Replace("e", "f");
```
✅
```csharp
var sb = new StringBuilder(input.Length);
// single pass replacing all patterns
```

**Pattern 2: Chained Regex.Replace() calls**

❌
```csharp
public static string Underscore(this string input) =>
    Regex3.Replace(Regex2.Replace(Regex1.Replace(input, "$1_$2"), "$1_$2"), "_").ToLower();
```
✅
```csharp
return string.Create(totalLength, state, (span, s) => { /* write directly */ });
```

**Pattern 3: += string concatenation in loops**

❌
```csharp
string result = "";
foreach (var part in parts)
    result += separator + part;
```
✅
```csharp
var sb = new StringBuilder();
foreach (var part in parts)
    sb.Append(separator).Append(part);
return sb.ToString();
```

**Impact: Eliminates N-1 intermediate string allocations per chain. For `+=` in loops, eliminates O(n²) total allocation.**

### Cache char.ToString() for Known Character Sets
🟡 **DO** cache `char.ToString()` results when the set of characters is small and known | .NET Core+

❌
```csharp
return symbol.ToString();

foreach (var prefix in UnitPrefixes)
    input = input.Replace(prefix.Value.Name, prefix.Key.ToString());
```
✅
```csharp
private static readonly FrozenDictionary<char, string> s_charStrings =
    new Dictionary<char, string>
    {
        ['k'] = "k", ['M'] = "M", ['G'] = "G",
    }.ToFrozenDictionary();

return s_charStrings[symbol];
```

**Impact: Eliminates one string allocation per char.ToString() call. Significant when called in loops or on hot paths.**

## Detection

Scan recipes for memory and string anti-patterns. Run these and report exact counts.

```bash
# .ToLower()/.ToUpper() without culture parameter (allocates + culture-sensitive)
grep -rn --include='*.cs' -E '\.(ToLower|ToUpper)\(\)' --exclude-dir=bin --exclude-dir=obj . | wc -l

# Chained .Replace( calls (3+ on one line — intermediate string allocations)
grep -rn --include='*.cs' '\.Replace(.*\.Replace(.*\.Replace(' --exclude-dir=bin --exclude-dir=obj . | wc -l

# params in method signatures (array allocation per call)
grep -rn --include='*.cs' 'params ' --exclude-dir=bin --exclude-dir=obj . | wc -l

# LINQ on strings — .All/.Any on IEnumerable<char> (replace with foreach loop)
grep -rn --include='*.cs' -E '\.(All|Any)\(char\.' --exclude-dir=bin --exclude-dir=obj . | wc -l
```

### Patterns Requiring Manual Review

- **Boxing via string.Format**: Can't determine argument types from grep — needs type analysis
- **`+=` string concatenation in loops**: `+=` matches all types (int, list, event, string) — needs type context to confirm string
- **`char.ToString()`**: Requires knowing the variable type is `char` — not reliably greppable
