using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var cache = new Dictionary<string, byte[]>();
var holders = new List<Holder>();
var pinnedHandles = new List<GCHandle>();

app.MapGet("/cache/{key}", (string key) =>
{
    if (!cache.ContainsKey(key))
    {
        cache[key] = new byte[200_000];
    }
    return "cached";
});

app.MapGet("/holders", () =>
{
    if (holders.Count == 0)
    {
        for (int i = 0; i < 100_000; i++)
            holders.Add(new Holder());
    }

    foreach (var h in holders)
        h.Ref = new object();

    return "updated";
});

app.MapGet("/pin", () =>
{
    var arr = new byte[1000];
    var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
    pinnedHandles.Add(handle);
    return $"pinned {pinnedHandles.Count}";
});

app.MapGet("/health", () => "healthy");

app.Run();

class Holder { public object? Ref; }
