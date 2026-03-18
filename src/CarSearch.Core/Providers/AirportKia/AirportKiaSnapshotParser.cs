using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.AirportKia;

public class AirportKiaSnapshotParser
{
    public string? FindFilterRef(string yaml, string label)
    {
        var pattern = $@"generic\s+\[ref=([^\]]+)\]\s+\[cursor=pointer\]:\s+{Regex.Escape(label)}";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public int ParseResultCount(string yaml)
    {
        var pattern = @"generic\s+\[ref=[^\]]+\]:\s+""(\d+)""";
        var matches = Regex.Matches(yaml, pattern);
        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[1].Value, out var count) && count > 0 && count < 10000)
                return count;
        }
        return 0;
    }

    public string? ParseCity(string yaml)
    {
        var pattern = @"in\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');
        var yearLinkPattern = @"link\s+""(?:New|Used|Pre-Owned|Certified)?\s*(\d{4})""\s*\[ref=([^\]]+)\]";
        var modelLinkPattern = @"link\s+""([^""]+)""\s*\[ref=([^\]]+)\]\s*\[cursor=pointer\]";
        var urlPattern = @"/url:\s+(/(?:view|inventory|vehicles?)/[^\s]+)";
        var pricePattern = @"(?:generic|text)\s+(?:\[ref=[^\]]+\]:\s+)?(\$[\d,]+)";
        var enginePattern = @"generic\s+\[ref=[^\]]+\]:\s+(\d+\s+Cylinder\s+Engine)";
        var drivetrainPattern = @"generic\s+\[ref=[^\]]+\]:\s+(AWD|4x4|FWD|RWD|4WD)";
        int i = 0;
        while (i < lines.Length)
        {
            var yearMatch = Regex.Match(lines[i], yearLinkPattern);
            if (!yearMatch.Success) { i++; continue; }
            var listing = new VehicleListing();
            listing.Year = int.Parse(yearMatch.Groups[1].Value);
            var blockEnd = Math.Min(i + 40, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);
            var urlMatch = Regex.Match(block, urlPattern);
            if (urlMatch.Success)
                listing.Url = "https://www.airportkia.ca" + urlMatch.Groups[1].Value;
            var modelMatches = Regex.Matches(block, modelLinkPattern);
            foreach (Match mm in modelMatches)
            {
                var text = mm.Groups[1].Value;
                if (text != "Learn More" && text != "Check Availability" && !text.StartsWith("STOCK") &&
                    !text.StartsWith("New") && !text.StartsWith("Used") && !text.Contains("$") &&
                    !text.StartsWith("Exterior") && !text.StartsWith("noopener") && text.Length > 3)
                {
                    listing.Title = $"{listing.Year} {text.Split('\n')[0].Trim()}";
                    break;
                }
            }
            if (string.IsNullOrEmpty(listing.Title))
                listing.Title = $"{listing.Year} Vehicle";
            var priceMatch = Regex.Match(block, pricePattern);
            if (priceMatch.Success)
                listing.Price = priceMatch.Groups[1].Value;
            var engineMatch = Regex.Match(block, enginePattern);
            if (engineMatch.Success)
                listing.FuelType = engineMatch.Groups[1].Value;
            var driveMatch = Regex.Match(block, drivetrainPattern);
            if (driveMatch.Success)
                listing.Transmission = driveMatch.Groups[1].Value;
            listing.Dealer = "Airport Kia";
            listing.Source = "AirportKia";
            if (listing.Year >= 2000 && !string.IsNullOrEmpty(listing.Title))
                listings.Add(listing);
            i++;
        }
        return listings;
    }
}
