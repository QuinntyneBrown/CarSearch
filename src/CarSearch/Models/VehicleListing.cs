namespace CarSearch.Models;

public class VehicleListing
{
    public string Title { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string? PriceRating { get; set; }
    public string Mileage { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Dealer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public string? MsrpPrice { get; set; }
    public int Year { get; set; }
    public string Source { get; set; } = string.Empty;
}
