namespace CarSearch.Models;

public class SearchParameters
{
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required string PostalCode { get; set; }
    public string? Color { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public string? OutputPath { get; set; }
    public int TimeoutMs { get; set; } = 15000;
}
