using CarSearch.Providers.Platforms.D2cMedia;
using Xunit;

namespace CarSearch.Core.Tests.Providers.Platforms.D2cMedia;

public class D2cMediaSnapshotParserTests
{
    private readonly D2cMediaSnapshotParser _parser = new();
    private readonly string _fixture = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "d2c-snapshot.yml"));

    [Fact]
    public void FindListItemRef_ReturnsMatchingRef()
    {
        var makeRef = _parser.FindListItemRef(_fixture, "BMW");
        var modelRef = _parser.FindListItemRef(_fixture, "X3");

        Assert.Equal("eMake", makeRef);
        Assert.Equal("eModel", modelRef);
    }

    [Fact]
    public void FindColorRef_ReturnsMatchingRef()
    {
        var colorRef = _parser.FindColorRef(_fixture, "White");

        Assert.Equal("eColor", colorRef);
    }

    [Fact]
    public void ParseCity_ReturnsHeadingCity()
    {
        var city = _parser.ParseCity(_fixture);

        Assert.Equal("Mississauga", city);
    }

    [Fact]
    public void ParseResultCount_UsesButtonCount()
    {
        var count = _parser.ParseResultCount(_fixture);

        Assert.Equal(12, count);
    }

    [Fact]
    public void ParseListings_BuildsListingsFromSharedPattern()
    {
        var context = new D2cMediaListingContext(
            "BMW Etobicoke",
            "BmwEtobicoke",
            "https://www.bmwetobicoke.com");

        var listings = _parser.ParseListings(_fixture, context);

        Assert.Equal(2, listings.Count);

        var first = listings[0];
        Assert.Equal(2023, first.Year);
        Assert.Equal("2023 BMW X3 xDrive30i", first.Title);
        Assert.Equal("Mississauga", first.Location);
        Assert.Equal("BMW Etobicoke", first.Dealer);
        Assert.Equal("BmwEtobicoke", first.Source);
        Assert.Equal("https://www.bmwetobicoke.com/used/2023-bmw-x3-id123.html", first.Url);
        Assert.Equal("$54,995", first.Price);
        Assert.Equal("12,345 km", first.Mileage);
        Assert.Equal("Automatic", first.Transmission);
    }
}
