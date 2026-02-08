# Dependency Injection Patterns

## Service Registration
```csharp
var services = new ServiceCollection();

// Transient - new instance each time
services.AddTransient<IMyService, MyService>();

// Scoped - one per scope
services.AddScoped<IMyService, MyService>();

// Singleton - one for app lifetime
services.AddSingleton<IMyService, MyService>();

// Factory registration
services.AddTransient<IMyService>(sp => new MyService(sp.GetRequiredService<IDep>()));

// Keyed services (.NET 8+)
services.AddKeyedSingleton<IMyService, MyServiceA>("a");
services.AddKeyedSingleton<IMyService, MyServiceB>("b");
```

## Configuration Binding
```csharp
// In setup
services.Configure<MyOptions>(configuration.GetSection("MySection"));

// In class
public class MyService(IOptions<MyOptions> options)
{
    private readonly MyOptions _options = options.Value;
}
```

## Logging
```csharp
public class MyService(ILogger<MyService> logger)
{
    public void DoWork()
    {
        logger.LogInformation("Processing {ItemId}", itemId);
        logger.LogError(ex, "Failed to process {ItemId}", itemId);
    }
}
```

## HttpClient Registration
```csharp
services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
```

## Hosted Services
```csharp
services.AddHostedService<MyBackgroundService>();

public class MyBackgroundService(ILogger<MyBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // work
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```
