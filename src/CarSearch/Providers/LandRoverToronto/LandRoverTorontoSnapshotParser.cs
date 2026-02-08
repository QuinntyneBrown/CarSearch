using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.LandRoverToronto;

public class LandRoverTorontoSnapshotParser
{
    public string? FindFilterRef(string yaml, string label)
    {
        var pattern = $@"generic\s+\[ref=([^\]]+)\]\s+\[cursor=pointer\]:\s+{Regex.Escape(label)}";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public int ParseResultCount(string yaml)
    {
        var pattern = @"generic\s+\[ref=[^\]]+\]:\s+""(\d+)""\s*\n\s*-\s*generic\s+\[ref=[^\]]+\]:\s+Results";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public string? ParseCity(string yaml)
    {
        return "Toronto";
    }

    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        // Year link: link "used 2024" or link "new 2025" (lowercase)
        var yearLinkPattern = @"link\s+""(new|used|pre-owned)\s+(\d{4})""\s*\[ref=([^\]]+)\]";
        var urlPattern = @"/url:\s+(/view/[^\s]+)";
        var pricePattern = @"generic\s+\[ref=[^\]]+\]:\s+\$([\d,]+)";
        var mileagePattern = @"generic\s+\[ref=[^\]]+\]:\s+([\d,]+)\s+km";
        // Full description link: link "Used 2024 Land Rover Discovery Sport Dynamic SE LR9664"
        var fullDescPattern = @"link\s+""(?:Used|New|Pre-Owned)\s+\d{4}\s+(.+?)\s+\w+\d+""\s*\[ref=";

        int i = 0;
        while (i < lines.Length)
        {
            var yearMatch = Regex.Match(lines[i], yearLinkPattern, RegexOptions.IgnoreCase);
            if (!yearMatch.Success) { i++; continue; }

            var listing = new VehicleListing();
            listing.Year = int.Parse(yearMatch.Groups[2].Value);
            listing.IsNew = yearMatch.Groups[1].Value.Equals("new", StringComparison.OrdinalIgnoreCase);

            var blockEnd = Math.Min(i + 30, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            var urlMatch = Regex.Match(block, urlPattern);
            if (urlMatch.Success)
                listing.Url = "https://landrovertoronto.ca" + urlMatch.Groups[1].Value;

            // Try to get title from the full description link (e.g. "Used 2024 Land Rover Discovery Sport Dynamic SE LR9664")
            var fullDescMatch = Regex.Match(block, fullDescPattern);
            if (fullDescMatch.Success)
            {
                listing.Title = $"{listing.Year} {fullDescMatch.Groups[1].Value.Trim()}";
            }
            else
            {
                // Fallback: get model from second link in block (after year link)
                var modelPattern = @"link\s+""([^""]+)""\s*\[ref=[^\]]+\]";
                var modelMatches = Regex.Matches(block, modelPattern);
                foreach (Match mm in modelMatches)
                {
                    var text = mm.Groups[1].Value;
                    if (text != "Learn More" && text != "Confirm Availability" &&
                        !Regex.IsMatch(text, @"^(new|used|pre-owned)\s+\d{4}$", RegexOptions.IgnoreCase) &&
                        !text.Contains("$") && !text.StartsWith("STOCK"))
                    {
                        listing.Title = $"{listing.Year} {text.Split('\n')[0].Trim()}";
                        break;
                    }
                }
            }

            var priceMatch = Regex.Match(block, pricePattern);
            if (priceMatch.Success)
                listing.Price = "$" + priceMatch.Groups[1].Value;

            var mileageMatch = Regex.Match(block, mileagePattern);
            if (mileageMatch.Success)
                listing.Mileage = mileageMatch.Groups[1].Value + " km";

            listing.Dealer = "Land Rover Toronto";
            listing.Location = "Toronto, ON";
            listing.Source = "LandRoverToronto";

            if (listing.Year >= 2000 && !string.IsNullOrEmpty(listing.Title))
                listings.Add(listing);

            i++;
        }

        return listings;
    }
}
