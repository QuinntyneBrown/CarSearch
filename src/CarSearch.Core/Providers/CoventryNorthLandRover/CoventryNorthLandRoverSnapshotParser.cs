using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.CoventryNorthLandRover;

public class CoventryNorthLandRoverSnapshotParser
{
    /// <summary>
    /// Find a clickable filter dropdown ref by its label.
    /// e.g. generic [ref=e114] [cursor=pointer]: Model
    /// </summary>
    public string? FindFilterRef(string yaml, string label)
    {
        var pattern = $@"generic\s+\[ref=([^\]]+)\]\s+\[cursor=pointer\]:\s+{Regex.Escape(label)}";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Parse the result count from the vehicle count display.
    /// e.g. generic [ref=e89]: "75"
    /// </summary>
    public int ParseResultCount(string yaml)
    {
        var pattern = @"text:\s+Vehicles\s*\n\s*-\s*generic\s+\[ref=[^\]]+\]:\s+""(\d+)""";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public string? ParseCity(string yaml)
    {
        // Try to find location from listing context
        var pattern = @"in\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)""";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : "Woodbridge";
    }

    /// <summary>
    /// Parse vehicle listings from LeadBox platform snapshot.
    /// Listings have: link "New YEAR" or link "Used YEAR",
    /// link "Model Trim", price link "$XX,XXX +tax & lic",
    /// specs link with exterior colour/engine/drivetrain.
    /// </summary>
    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        // Pattern for the year link: link "Used 2025 Land Rover" or link "New 2025 Land Rover"
        var yearLinkPattern = @"link\s+""(New|Used|Pre-Owned)\s+(\d{4})[^""]*""\s*\[ref=([^\]]+)\]";
        // Pattern for model/trim link: link "Range Rover Sport Dynamic SE"
        var modelLinkPattern = @"link\s+""([^""]+)""\s*\[ref=([^\]]+)\]\s*\[cursor=pointer\]";
        // Pattern for URL
        var urlPattern = @"/url:\s+(/view/[^\s]+)";
        // Pattern for price: generic [ref=...]: $XX,XXX
        var priceGenericPattern = @"generic\s+\[ref=[^\]]+\]:\s+(\$[\d,]+)";
        // Pattern for engine
        var enginePattern = @"generic\s+\[ref=[^\]]+\]:\s+(\d+\s+Cylinder\s+Engine)";
        // Pattern for drivetrain
        var drivetrainPattern = @"generic\s+\[ref=[^\]]+\]:\s+(AWD|4x4|FWD|RWD|4WD)";

        int i = 0;
        while (i < lines.Length)
        {
            var yearMatch = Regex.Match(lines[i], yearLinkPattern);
            if (!yearMatch.Success) { i++; continue; }

            var listing = new VehicleListing();
            var condition = yearMatch.Groups[1].Value;
            listing.Year = int.Parse(yearMatch.Groups[2].Value);
            listing.IsNew = condition == "New";

            // Look ahead for model, price, URL, specs
            var blockEnd = Math.Min(i + 40, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            // Find URL
            var urlMatch = Regex.Match(block, urlPattern);
            if (urlMatch.Success)
                listing.Url = "https://coventrynorthlandrover.com" + urlMatch.Groups[1].Value;

            // Find model name from the second link after year
            var modelMatches = Regex.Matches(block, modelLinkPattern);
            foreach (Match mm in modelMatches)
            {
                var text = mm.Groups[1].Value;
                if (text != "Learn More" && text != "Check Availability" && !text.StartsWith("STOCK") &&
                    !text.StartsWith("New") && !text.StartsWith("Used") && !text.Contains("$") &&
                    !text.StartsWith("Exterior") && !text.StartsWith("noopener"))
                {
                    // Extract model and trim
                    var parts = text.Split('\n')[0].Trim();
                    listing.Title = $"{listing.Year} {parts}";
                    break;
                }
            }

            // Find price
            var priceMatch = Regex.Match(block, priceGenericPattern);
            if (priceMatch.Success)
                listing.Price = priceMatch.Groups[1].Value;

            // Find engine info
            var engineMatch = Regex.Match(block, enginePattern);
            if (engineMatch.Success)
                listing.FuelType = engineMatch.Groups[1].Value;

            // Find drivetrain
            var driveMatch = Regex.Match(block, drivetrainPattern);
            if (driveMatch.Success)
                listing.Transmission = driveMatch.Groups[1].Value;

            listing.Dealer = "Coventry North Land Rover";
            listing.Location = "Woodbridge, ON";
            listing.Source = "CoventryNorthLandRover";

            if (listing.Year >= 2000 && !string.IsNullOrEmpty(listing.Title))
                listings.Add(listing);

            i++;
        }

        return listings;
    }
}
