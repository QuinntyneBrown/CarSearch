using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.LandRoverMetroWest;

public class LandRoverMetroWestSnapshotParser
{
    public string? FindListItemRef(string yaml, string label)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(label)}[^""]*""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public string? FindColorRef(string yaml, string color)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(color)}""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public string? ParseCity(string yaml)
    {
        var pattern = @"heading\s+""[^""]*for\s+sale\s+in\s+([^""]+)""\s*\[level=1\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public int ParseResultCount(string yaml)
    {
        var pattern = @"button\s+""Used vehicles\s+(\d+)""";
        var match = Regex.Match(yaml, pattern);
        if (match.Success) return int.Parse(match.Groups[1].Value);
        // Fallback: count vehicle listing links
        var linkPattern2 = @"link\s+""(?:19|20)\d{2}\s+.+?\s+in\s+[^""]+""";
        return Regex.Matches(yaml, linkPattern2).Count;
    }

    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        var linkPattern = @"link\s+""((?:19|20)\d{2})\s+(.+?)\s+in\s+([^""]+)""\s*\[ref=([^\]]+)\]";
        var urlPattern = @"/url:\s+(/(?:used|new)/\d{4}-[^\s]+\.html)";
        var specsPattern = @"""([\d,]+)\s+KM\.\s+([^,]+),\s+Ext:\s+([^,""]+)";
        var pricePattern = @"text:\s+([\d,]+)\s+\$";

        int i = 0;
        while (i < lines.Length)
        {
            var linkMatch = Regex.Match(lines[i], linkPattern);
            if (!linkMatch.Success) { i++; continue; }

            var listing = new VehicleListing
            {
                Year = int.Parse(linkMatch.Groups[1].Value),
                Title = $"{linkMatch.Groups[1].Value} {linkMatch.Groups[2].Value}",
                Location = linkMatch.Groups[3].Value,
                Dealer = "Land Rover Metro West",
                Source = "LandRoverMetroWest"
            };

            var blockEnd = Math.Min(i + 30, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            var urlMatch = Regex.Match(block, urlPattern);
            if (urlMatch.Success)
                listing.Url = "https://www.landrovermetrowest.ca" + urlMatch.Groups[1].Value;

            var priceMatch = Regex.Match(block, pricePattern);
            if (priceMatch.Success)
                listing.Price = "$" + priceMatch.Groups[1].Value;

            var specsMatch = Regex.Match(block, specsPattern);
            if (specsMatch.Success)
            {
                listing.Mileage = specsMatch.Groups[1].Value + " km";
                listing.Transmission = specsMatch.Groups[2].Value.Trim();
            }

            if (listing.Year >= 2000)
                listings.Add(listing);

            i++;
        }

        return listings;
    }
}
