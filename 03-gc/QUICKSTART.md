# GC Playground - Quick Start Guide

## 🚀 Getting Started in 5 Minutes

### Step 1: Install Prerequisites

```bash
# Install diagnostic tools
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-dump
```

### Step 2: Run the Main Console Application

```bash
cd GcPlayground.Console
dotnet run
```

You'll see a menu with 12 different GC scenarios to explore.

### Step 3: Monitor GC Metrics (in another terminal)

```bash
# Get the process ID from the running application
# Then run:
dotnet-counters monitor --process-id <PID> System.Runtime
```

## 📚 Scenario Overview

### Block 1: Generations (Options 1-3)
- **Option 1**: Gen0 Allocations - See how short-lived objects are collected
- **Option 2**: Object Promotion - Watch objects move between generations
- **Option 3**: Memory Leak - Simulate a real memory leak scenario

### Block 2: Large Object Heap (Options 4-6)
- **Option 4**: LOH Allocations - Objects > 85KB behavior
- **Option 5**: LOH Fragmentation - See heap fragmentation issues
- **Option 6**: LOH Compaction - Manual compaction effects

### Block 3: Latency (Options 7-8)
- **Option 7**: Latency Test - Measure GC impact on response times
- **Option 8**: Server vs Workstation - Compare GC modes

### Block 4: Card Table (Options 9-10)
- **Option 9**: Simple Test - Basic Gen2→Gen0 references
- **Option 10**: Stress Test - Performance impact of many references

### Block 5: Pinning (Options 11-12)
- **Option 11**: Fixed Pinning - Short-term pinning with `fixed`
- **Option 12**: GCHandle Pinning - Long-term pinning effects

## 🌐 Web Applications

### BadService (Anti-patterns)
```bash
cd GcPlayground.BadService
dotnet run
```

Demonstrates common GC anti-patterns:
- Eternal cache
- Gen2→Gen0 references
- Object pinning

### GoodService (Best Practices)
```bash
cd GcPlayground.GoodService
dotnet run
```

Shows optimized approaches:
- TTL-based cache
- ArrayPool usage
- Value types
- Stack allocation

## 📊 Key Metrics to Watch

When running `dotnet-counters monitor`:

| Metric | Meaning |
|--------|---------|
| `gen-0-gc-count` | Number of Gen0 collections |
| `gen-1-gc-count` | Number of Gen1 collections |
| `gen-2-gc-count` | Number of Gen2 collections |
| `alloc-rate` | Bytes allocated per second |
| `loh-size` | Large Object Heap size |
| `time-in-gc` | Percentage of time in GC |

## 💡 Typical Workflow

1. **Start the scenario** - Run one of the 12 options
2. **Monitor metrics** - Watch dotnet-counters in another terminal
3. **Observe behavior** - Note how metrics change
4. **Analyze results** - Compare with expectations from the plan
5. **Repeat** - Try different scenarios to understand patterns

## 🔍 Debugging Tips

### Memory Growing Unexpectedly?
- Check Gen2 collection count
- Look for eternal caches or pinned objects
- Use `dotnet-dump` to analyze heap

### Latency Spikes?
- Monitor GC pause times
- Check if Gen2 collections are happening
- Consider Server vs Workstation GC mode

### High Allocation Rate?
- Look for unnecessary object creation
- Consider using ArrayPool or stackalloc
- Profile with dotnet-trace

## 📖 Next Steps

1. Run all 12 scenarios to understand GC behavior
2. Compare BadService vs GoodService performance
3. Experiment with different GC modes
4. Read the detailed README.md for in-depth explanations
5. Study the implementation plan for theoretical background

## 🆘 Common Issues

**"Process not found"** when using dotnet-counters
- Make sure the application is still running
- Use `Get-Process dotnet` (PowerShell) to find the PID

**"Unsafe code" compilation error**
- Already fixed in the project configuration
- Make sure you're using the latest build

**Web app won't start**
- Check if port 5000 is already in use
- Try running on a different port with `--urls http://localhost:5001`

## 📚 Resources

- [.NET GC Documentation](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/)
- [dotnet-counters Guide](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [Pro .NET Memory Management](https://prodotnetmemory.com/)

---

**Happy GC Learning! 🎓**
