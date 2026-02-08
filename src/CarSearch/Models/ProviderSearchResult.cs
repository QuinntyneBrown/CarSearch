namespace CarSearch.Models;

public class ProviderSearchResult
{
    public required string ProviderName { get; set; }
    public required string DisplayName { get; set; }
    public List<VehicleListing> Listings { get; set; } = [];
    public int TotalCount { get; set; }
    public string? City { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}
