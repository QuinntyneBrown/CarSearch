using CarSearch.Models;

namespace CarSearch.Providers.Platforms.D2cMedia;

public interface ID2cMediaSnapshotParser
{
    string? FindListItemRef(string yaml, string label);
    string? FindColorRef(string yaml, string color);
    string? ParseCity(string yaml);
    int ParseResultCount(string yaml);
    List<VehicleListing> ParseListings(string yaml, D2cMediaListingContext context);
}
