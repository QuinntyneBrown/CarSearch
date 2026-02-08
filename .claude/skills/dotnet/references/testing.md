# Testing Patterns (.NET)

## Test Project Setup
```bash
dotnet new xunit -n AutoTraderSearch.Tests
dotnet add AutoTraderSearch.Tests/AutoTraderSearch.Tests.csproj reference AutoTraderSearch/AutoTraderSearch.csproj
dotnet add AutoTraderSearch.Tests/AutoTraderSearch.Tests.csproj package Moq
dotnet add AutoTraderSearch.Tests/AutoTraderSearch.Tests.csproj package FluentAssertions
dotnet sln add AutoTraderSearch.Tests/AutoTraderSearch.Tests.csproj
```

## xUnit Basics
```csharp
public class MyServiceTests
{
    [Fact]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var sut = new MyService();

        // Act
        var result = await sut.DoWorkAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("expected", result.Value);
    }

    [Theory]
    [InlineData("input1", "expected1")]
    [InlineData("input2", "expected2")]
    public void Method_WithVariousInputs_ReturnsExpected(string input, string expected)
    {
        var result = MyService.Transform(input);
        Assert.Equal(expected, result);
    }
}
```

## Mocking with Moq
```csharp
var mockService = new Mock<IMyService>();
mockService.Setup(s => s.GetDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new MyData { Name = "Test" });

var sut = new Consumer(mockService.Object);
```

## FluentAssertions
```csharp
result.Should().NotBeNull();
result.Items.Should().HaveCount(3);
result.Name.Should().StartWith("Test");
action.Should().ThrowAsync<ArgumentException>();
```

## MockHttpMessageHandler
```csharp
var handler = new MockHttpMessageHandler(
    new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("""{"key":"value"}""", Encoding.UTF8, "application/json")
    });
var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.com") };
```

## Running Tests
```bash
dotnet test                                    # all tests
dotnet test --filter "Category=Unit"          # by category
dotnet test --filter "FullyQualifiedName~Search" # by name pattern
dotnet test --collect:"XPlat Code Coverage"    # with coverage
```
