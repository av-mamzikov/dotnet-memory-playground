using System.Buffers;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var cache = new ConcurrentDictionary<string, CacheEntry>();
var holders = new List<HolderStruct>();

app.MapGet("/cache/{key}", (string key) =>
{
    var now = DateTime.UtcNow;
    foreach (var k in cache.Keys.ToList())
    {
        if (cache.TryGetValue(k, out var entry) && entry.ExpiresAt < now)
            cache.TryRemove(k, out _);
    }

    if (!cache.TryGetValue(key, out var cached))
    {
        cached = new CacheEntry(
            new byte[200_000],
            DateTime.UtcNow.AddMinutes(5)
        );
        cache[key] = cached;
    }

    return "cached";
});

app.MapGet("/pooled", () =>
{
    var buffer = ArrayPool<byte>.Shared.Rent(200_000);
    try
    {
        return "ok";
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
});

app.MapGet("/holders", () =>
{
    if (holders.Count == 0)
    {
        for (int i = 0; i < 100_000; i++)
            holders.Add(new HolderStruct { Value = i });
    }

    return "updated";
});

app.MapGet("/span", () =>
{
    Span<byte> buffer = stackalloc byte[1000];
    return "ok";
});

app.MapGet("/health", () => "healthy");

app.Run();

record CacheEntry(byte[] Data, DateTime ExpiresAt);

struct HolderStruct
{
    public int Value;
}
