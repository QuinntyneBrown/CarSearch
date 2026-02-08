# Async & HTTP Patterns

## HttpClient Best Practices
```csharp
// Use IHttpClientFactory (registered via DI)
public class ApiClient(HttpClient httpClient)
{
    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    public async Task<string> GetStringAsync(string url, CancellationToken ct = default)
    {
        return await httpClient.GetStringAsync(url, ct);
    }

    public async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(url, data, ct);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
```

## Retry with Polly (if added)
```csharp
services.AddHttpClient<ApiClient>()
    .AddStandardResilienceHandler();
// Or configure custom:
    .AddResilienceHandler("custom", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential
        });
    });
```

## Async Enumerable (Streaming)
```csharp
public async IAsyncEnumerable<Item> StreamItemsAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var item in source.WithCancellation(ct))
    {
        yield return Transform(item);
    }
}
```

## Parallel Async Work
```csharp
// Bounded parallelism
var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct };
await Parallel.ForEachAsync(items, options, async (item, token) =>
{
    await ProcessAsync(item, token);
});

// Task.WhenAll for small sets
var tasks = items.Select(i => ProcessAsync(i, ct));
var results = await Task.WhenAll(tasks);
```

## Cancellation
```csharp
public async Task DoWorkAsync(CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    await SomeOperationAsync(ct);
}
```

## JSON Serialization
```csharp
// System.Text.Json
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var obj = JsonSerializer.Deserialize<MyType>(json, options);
var json = JsonSerializer.Serialize(obj, options);

// Source generators for AOT
[JsonSerializable(typeof(MyType))]
internal partial class AppJsonContext : JsonSerializerContext { }
```
