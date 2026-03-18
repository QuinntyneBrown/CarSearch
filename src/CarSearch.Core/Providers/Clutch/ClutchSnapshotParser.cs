using CarSearch.Models;

namespace CarSearch.Providers.Clutch;

public class ClutchSnapshotParser
{
    public List<VehicleListing> ParseListings(string yaml)
    {
        // TODO: Implement Clutch.ca ARIA snapshot parsing
        return [];
    }

    public int ParseResultCount(string yaml)
    {
        // TODO: Implement result count parsing for Clutch.ca
        return 0;
    }

    public string? ParseCity(string yaml)
    {
        // TODO: Implement city parsing for Clutch.ca
        return null;
    }
}
