using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.BmwToronto;

public class BmwTorontoSnapshotParser
{
    public string? FindButtonRef(string yaml, string label)
    {
        var pattern = $@"button\s+""{Regex.Escape(label)}""\s*\[ref=([^\]]+)\]\s*\[cursor=pointer\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public string? FindColorRef(string yaml, string color)
    {
        return FindButtonRef(yaml, color);
    }

    public string? ParseCity(string yaml)
    {
        return "Toronto, ON";
    }

    public int ParseResultCount(string yaml)
    {
        // Format: generic [ref=...]: "70" \n text: VEHICLES AVAILABLE.
        var pattern = @"generic\s+\[ref=[^\]]+\]:\s+""(\d+)""\s*\n\s*-?\s*text:\s+VEHICLES AVAILABLE";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        // Match year line: strong [ref=...]: "2022"
        var yearPattern = @"strong\s+\[ref=[^\]]+\]:\s+""(\d{4})""";

        int i = 0;
        while (i < lines.Length)
        {
            var yearMatch = Regex.Match(lines[i], yearPattern);
            if (!yearMatch.Success) { i++; continue; }

            // Check if next line has model text like "text: BMW X3 xDrive30i"
            if (i + 1 >= lines.Length) { i++; continue; }
            var modelLine = lines[i + 1].Trim();
            var modelTextMatch = Regex.Match(modelLine, @"^-?\s*text:\s+(.+)$");
            if (!modelTextMatch.Success) { i++; continue; }

            var year = int.Parse(yearMatch.Groups[1].Value);
            var modelText = modelTextMatch.Groups[1].Value.Trim();

            // Skip if this isn't a vehicle model (e.g. "Powered By" or other text)
            if (year < 2000 || string.IsNullOrEmpty(modelText) || modelText.Contains("AVAILABLE")) { i++; continue; }

            var listing = new VehicleListing
            {
                Year = year,
                Title = $"{year} {modelText}",
                Dealer = "BMW Toronto",
                Location = "Toronto, ON",
                Source = "BmwToronto"
            };

            var blockEnd = Math.Min(i + 50, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            // URL: /url: /inventory-vehicle?sn=NN20285A
            var urlMatch = Regex.Match(block, @"/url:\s+(/inventory-vehicle\?sn=[^\s]+)");
            if (urlMatch.Success)
                listing.Url = "https://www.bmwtoronto.ca" + urlMatch.Groups[1].Value;

            // Price: strong [ref=...]: $31,495.00
            var priceMatch = Regex.Match(block, @"strong\s+\[ref=[^\]]+\]:\s+\$([\d,.]+)");
            if (priceMatch.Success)
                listing.Price = "$" + priceMatch.Groups[1].Value;

            // Mileage: generic [ref=...]: 106,799 km (after MILEAGE label)
            var mileageMatch = Regex.Match(block, @"MILEAGE\s*\n\s*-\s*generic\s+\[ref=[^\]]+\]:\s+([\d,]+)\s+km");
            if (mileageMatch.Success)
                listing.Mileage = mileageMatch.Groups[1].Value + " km";

            // Exterior color: generic [ref=...]: Jet Black (after EXTERIOR label)
            var extMatch = Regex.Match(block, @"EXTERIOR\s*\n\s*-\s*generic\s+\[ref=[^\]]+\]:\s+(.+)");
            if (extMatch.Success)
                listing.FuelType = extMatch.Groups[1].Value.Trim(); // Using FuelType for exterior color

            // Check if CPO
            if (block.Contains("Certified Pre-Owned"))
                listing.IsNew = false;

            if (listing.Year >= 2000 && !string.IsNullOrEmpty(listing.Title))
                listings.Add(listing);

            i++;
        }

        return listings;
    }
}
