# .NET / C# Development Skill

## Overview
Provides C# and .NET expertise for building, testing, debugging, and maintaining .NET projects.

## Project Context
- **SDK**: .NET 10 (targeting net9.0)
- **Project type**: Console application with `System.CommandLine`
- **DI/Config**: Microsoft.Extensions.DependencyInjection, Configuration, Logging, Options
- **Nullable**: Enabled
- **Implicit usings**: Enabled

## Common Commands

### Build & Run
```bash
dotnet build src/CarSearch/CarSearch.csproj
dotnet run --project src/CarSearch/CarSearch.csproj
dotnet run --project src/CarSearch/CarSearch.csproj -- [args]
```

### NuGet Packages
```bash
dotnet add src/CarSearch/CarSearch.csproj package <PackageName>
dotnet remove src/CarSearch/CarSearch.csproj package <PackageName>
dotnet list src/CarSearch/CarSearch.csproj package
dotnet restore
```

### Testing
```bash
dotnet test
dotnet test --filter "FullyQualifiedName~TestClass"
dotnet test --verbosity detailed
```

### Code Quality
```bash
dotnet format src/CarSearch/CarSearch.csproj
dotnet build --warnaserrors
```

## C# Coding Conventions (This Project)

### Style
- Use file-scoped namespaces (`namespace Foo;`)
- Use primary constructors where appropriate (C# 12+)
- Prefer `var` for local variables when the type is obvious
- Use `readonly` and `required` modifiers where appropriate
- Use expression-bodied members for single-line methods/properties
- Use pattern matching (`is`, `switch` expressions) over type casting
- Use collection expressions `[1, 2, 3]` (C# 12+)
- Nullable reference types are enabled — avoid `null!` suppression; handle nullability properly

### Naming
- **PascalCase**: Types, methods, properties, events, public fields
- **camelCase**: Parameters, local variables
- **_camelCase**: Private fields (with underscore prefix)
- **I-prefix**: Interfaces (`IService`)
- **Async suffix**: Async methods (`GetDataAsync`)

### Architecture Patterns
- Constructor injection for dependencies
- `IOptions<T>` / `IOptionsSnapshot<T>` for configuration binding
- `ILogger<T>` for structured logging
- Async/await throughout (avoid `.Result` or `.Wait()`)
- Cancellation token propagation on async methods

### Error Handling
- Throw specific exceptions, not `Exception`
- Use guard clauses (`ArgumentNullException.ThrowIfNull()`)
- Let exceptions propagate unless you can handle them meaningfully

## References
- See `references/` folder for detailed topic guides
