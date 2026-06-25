---
description: >-
  Performance measurement guide for dotnet-performance skill. Covers tool
  selection per category, KPI targets, BenchmarkDotNet, k6 load testing,
  CI/CD integration, and live process CLI commands.
metadata:
  tags: [measurement, benchmarkdotnet, k6, dotnet-counters, kpi]
---

# Measurement Guide

How to measure performance before and after applying optimizations. Never optimize without baseline data.

---

## Tool Selection Decision Table

Map each optimization category to the appropriate measurement tools:

| Category | Primary Tool | Secondary Tool | What to Measure |
|---|---|---|---|
| MEM | `dotnet-counters` (gc-heap-size, alloc-rate) | `dotnet-gcdump` comparison | Allocation rate reduction, GC collection frequency |
| ASYNC | `dotnet-counters` (threadpool-queue-length, thread-count) | App Insights dependency tracking | Thread pool starvation, blocked threads |
| LINQ | BenchmarkDotNet `[MemoryDiagnoser]` | `dotnet-trace` hot path | Allocation per operation, throughput |
| DB | `response.RequestCharge` logging | App Insights DB dependency | RU cost per operation, query latency |
| JSON | BenchmarkDotNet serialization benchmark | `dotnet-counters` alloc-rate | Throughput (ops/sec), bytes allocated |
| CACHE | App Insights dependency duration | Custom hit ratio counter | Cache hit rate, dependency call reduction |
| DI | `dotnet-counters` alloc-rate | Load test comparison | Object creation overhead |
| CONC | `dotnet-counters` (monitor-lock-contention-count) | `dotnet-trace` contention events | Lock wait time, throughput under load |
| HTTP | `dotnet-counters` Microsoft.AspNetCore.Hosting | k6/NBomber load test | Request duration, throughput |
| EXC | `dotnet-counters` exception-count | App Insights exceptions | Exception rate per interval |
| RESP | Network tab / curl with timing | k6 response size check | Response size (bytes), transfer time |
| STR | BenchmarkDotNet `[MemoryDiagnoser]` | `dotnet-counters` alloc-rate | String allocations per operation |
| STARTUP | Startup time measurement | `dotnet-trace` startup events | Time to first request, cold start latency |
| METRICS | `MetricCollector<T>` in tests | Prometheus/Grafana dashboard | Metric emission, cardinality |

---

## KPI Targets

Standard targets for ASP.NET Core APIs. Use as thresholds when evaluating optimization impact:

| Metric | Target | Red Flag |
|---|---|---|
| P50 response time | < 100ms | > 200ms |
| P95 response time | < 500ms | > 1000ms |
| P99 response time | < 1000ms | > 2000ms |
| Error rate (5xx) | < 0.1% | > 1% |
| CPU utilization | < 70% sustained | > 85% |
| Memory working set | < 80% | > 90% |
| Thread pool queue length | < 10 sustained | > 50 |
| GC time percentage | < 10% | > 20% |
| Allocation rate | Trend down after optimization | Sustained increase |

---

## BenchmarkDotNet Guidance

Use for micro-optimizations on hot paths (MEM, LINQ, JSON, STR categories).

**When to benchmark**: Hot-path changes where the difference is in nanoseconds or bytes allocated. Not needed for architectural changes (caching, DI lifetime) — use load testing instead.

**Minimum setup**:
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class MyBenchmark
{
    [Benchmark(Baseline = true)]
    public void Original() { /* original code */ }

    [Benchmark]
    public void Optimized() { /* optimized code */ }
}
```

**Run command**: `dotnet run -c Release --project path/to/benchmark`

**Common pitfalls**:
- Running in Debug mode (JIT optimizations disabled, results meaningless)
- Not returning computed values (JIT eliminates dead code)
- Ignoring allocation metrics (throughput may improve but allocations increase)
- Benchmarking with a debugger attached
- Including setup costs in the measured method

---

## Load Testing

For HIGH-impact optimizations, perform before/after load testing to validate real-world improvement.

**k6 template**:
```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 20 },
        { duration: '1m', target: 20 },
        { duration: '10s', target: 0 },
    ],
    thresholds: {
        http_req_duration: ['p(50)<100', 'p(95)<500', 'p(99)<1000'],
        http_req_failed: ['rate<0.01'],
    },
};

export default function () {
    const res = http.get('http://localhost:5000/your-endpoint');
    check(res, {
        'status is 200': (r) => r.status === 200,
        'p95 under 500ms': (r) => r.timings.duration < 500,
    });
    sleep(1);
}
```

**While load testing, monitor simultaneously**:
```bash
dotnet-counters monitor -n <ProcessName> --counters System.Runtime,Microsoft.AspNetCore.Hosting
```

---

## CI/CD Integration

For PR regression detection, use `benchmark-action/github-action-benchmark`:

```yaml
- uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: BenchmarkDotNet.Artifacts/results/*.json
    alert-threshold: '150%'
    comment-on-alert: true
    fail-on-alert: true
```

This fails the PR if any benchmark regresses by more than 50% compared to the baseline.

---

## Code Review Mode: Quick Reference Commands

```bash
# Baseline runtime health
dotnet-counters monitor -n <ProcessName> --counters System.Runtime

# ASP.NET Core request metrics
dotnet-counters monitor -n <ProcessName> --counters Microsoft.AspNetCore.Hosting

# Full monitoring (runtime + HTTP + custom meters)
dotnet-counters monitor -n <ProcessName> --counters System.Runtime,Microsoft.AspNetCore.Hosting,Microsoft.AspNetCore.Server.Kestrel

# GC heap snapshot for before/after comparison
dotnet-gcdump collect -n <ProcessName> -o before.gcdump
# ... apply optimization ...
dotnet-gcdump collect -n <ProcessName> -o after.gcdump

# 30-second CPU trace
dotnet-trace collect -n <ProcessName> --duration 00:00:30
dotnet-trace convert trace.nettrace --format speedscope
```

---

## Diagnostic Mode: Full CLI Commands

When profiling a live process (Mode A), use these commands by investigation stage:

```bash
# Stage 1: Live triage
dotnet-counters monitor -p <PID> --counters System.Runtime
dotnet-counters monitor -n <ProcessName> --counters System.Runtime,Microsoft.AspNetCore.Hosting,Microsoft.AspNetCore.Server.Kestrel

# Stage 2: Stuck/hung process — get stacks immediately
dotnet-stack report -p <PID>

# Stage 3: CPU + allocation hot paths
dotnet-trace collect -p <PID> --duration 00:00:30
dotnet-trace report <trace.nettrace> topN

# Stage 4: Heap composition
dotnet-gcdump collect -p <PID> -o before.gcdump
# ... apply optimization ...
dotnet-gcdump collect -p <PID> -o after.gcdump
dotnet-gcdump report <file.gcdump>

# Stage 5: Full dump for SOS analysis
dotnet-dump collect -p <PID> --type Heap
dotnet-dump analyze <dump> -c "dumpheap -stat" -c "exit"
```
