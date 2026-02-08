# System.CommandLine Patterns

## Basic Command Setup
```csharp
var rootCommand = new RootCommand("App description");

var nameOption = new Option<string>("--name", "Description") { IsRequired = true };
var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
rootCommand.AddOption(nameOption);
rootCommand.AddOption(verboseOption);

rootCommand.SetHandler((name, verbose) =>
{
    // handler logic
}, nameOption, verboseOption);

await rootCommand.InvokeAsync(args);
```

## Subcommands
```csharp
var searchCommand = new Command("search", "Search for items");
searchCommand.AddOption(queryOption);
searchCommand.SetHandler((query) => { /* ... */ }, queryOption);
rootCommand.AddCommand(searchCommand);
```

## Arguments vs Options
```csharp
// Argument: positional, required by default
var fileArg = new Argument<FileInfo>("file", "The file to process");

// Option: named, optional by default
var outputOption = new Option<string>("--output", () => "default", "Output path");
```

## With DI Integration
```csharp
var rootCommand = new RootCommand();
rootCommand.SetHandler(async (InvocationContext context) =>
{
    var services = new ServiceCollection();
    // register services...
    var provider = services.BuildServiceProvider();
    var myService = provider.GetRequiredService<IMyService>();
    await myService.RunAsync(context.GetCancellationToken());
});
```

## Common Patterns
```csharp
// Aliases
var option = new Option<string>(["--output", "-o"], "Output path");

// Validators
option.AddValidator(result =>
{
    var value = result.GetValueForOption(option);
    if (string.IsNullOrEmpty(value))
        result.ErrorMessage = "Value cannot be empty";
});

// Global options (apply to all subcommands)
rootCommand.AddGlobalOption(verboseOption);
```
