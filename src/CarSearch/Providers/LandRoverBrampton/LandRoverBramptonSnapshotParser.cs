using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.LandRoverBrampton;

public class LandRoverBramptonSnapshotParser
{
    /// <summary>
    /// Find a button ref by partial label text.
    /// e.g. button "Land Rover 70" [ref=e183]
    /// </summary>
    public string? FindButtonRef(string yaml, string label)
    {
        var pattern = $@"button\s+""{Regex.Escape(label)}[^""]*""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a listitem ref by its label text.
    /// e.g. listitem "Range Rover" [ref=e206] [cursor=pointer]
    /// </summary>
    public string? FindListItemRef(string yaml, string label)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(label)}[^""]*""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a color listitem ref by its name.
    /// e.g. listitem "White" [ref=e313] [cursor=pointer]
    /// </summary>
    public string? FindColorRef(string yaml, string color)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(color)}""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a spinbutton ref for year filtering (min or max).
    /// The page has two spinbuttons for year range.
    /// </summary>
    public (string? minRef, string? maxRef) FindYearSpinButtons(string yaml)
    {
        var pattern = @"spinbutton\s*\[ref=([^\]]+)\]:\s*""(\d{4})""";
        var matches = Regex.Matches(yaml, pattern);
        if (matches.Count >= 2)
        {
            return (matches[0].Groups[1].Value, matches[1].Groups[1].Value);
        }
        return (null, null);
    }

    /// <summary>
    /// Parse the heading to get the result context.
    /// e.g. heading "Used Vehicles for sale in Brampton" [level=1]
    /// </summary>
    public string? ParseCity(string yaml)
    {
        var pattern = @"heading\s+""[^""]*for\s+sale\s+in\s+([^""]+)""\s*\[level=1\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// Parse the result count from category buttons or heading.
    /// e.g. button "Used vehicles 30" or heading "Used Land Rover for Sale in Brampton"
    /// </summary>
    public int ParseResultCount(string yaml)
    {
        // Try button pattern first: button "Used vehicles 30"
        var pattern = @"button\s+""Used vehicles\s+(\d+)""";
        var match = Regex.Match(yaml, pattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        // Fallback: count the number of vehicle listing links
        var linkPattern = @"link\s+""(?:19|20)\d{2}\s+.+?\s+in\s+[^""]+""";
        return Regex.Matches(yaml, linkPattern).Count;
    }

    /// <summary>
    /// Parse all vehicle listings from the snapshot.
    /// Each listing has a descriptive link like:
    ///   link "2023 Range Rover Sport Dynamic SE ... in Brampton" [ref=...]
    /// followed by URL, price, and specs within the next ~30 lines.
    /// </summary>
    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        // Match descriptive vehicle links: link "YEAR Model Trim... in Location"
        var linkPattern = @"link\s+""((?:19|20)\d{2})\s+(.+?)\s+in\s+([^""]+)""\s*\[ref=([^\]]+)\]";
        // URL pattern: /used/YYYY-Make-Model-idNNN.html or /new/YYYY-...
        var urlPattern = @"/url:\s+(/(?:used|new)/\d{4}-[^\s]+\.html)";
        // Specs: "104,092 KM. Auto., Ext: White," or "85,474 KM. Auto., Ext: White, Int: Black"
        var specsPattern = @"""([\d,]+)\s+KM\.\s+([^,]+),\s+Ext:\s+([^,""]+)";
        // Price: text: 28,998 $
        var pricePattern = @"text:\s+([\d,]+)\s+\$";

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            var linkMatch = Regex.Match(line, linkPattern);
            if (!linkMatch.Success)
            {
                i++;
                continue;
            }

            var listing = new VehicleListing();
            var year = int.Parse(linkMatch.Groups[1].Value);
            var modelTrim = linkMatch.Groups[2].Value;
            var location = linkMatch.Groups[3].Value;

            listing.Year = year;
            listing.Title = $"{year} {modelTrim}";
            listing.Location = location;

            // Look ahead within the next ~30 lines for URL, price, specs
            var blockEnd = Math.Min(i + 30, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            // Extract URL
            var urlMatch = Regex.Match(block, urlPattern);
            if (urlMatch.Success)
            {
                listing.Url = "https://www.landroverbrampton.com" + urlMatch.Groups[1].Value;
            }

            // Extract price
            var priceMatch = Regex.Match(block, pricePattern);
            if (priceMatch.Success)
            {
                listing.Price = "$" + priceMatch.Groups[1].Value;
            }

            // Extract specs (KM, Transmission, Ext color)
            var specsMatch = Regex.Match(block, specsPattern);
            if (specsMatch.Success)
            {
                listing.Mileage = specsMatch.Groups[1].Value + " km";
                listing.Transmission = specsMatch.Groups[2].Value.Trim();
            }

            listing.Dealer = "Land Rover Brampton";
            listing.Source = "LandRoverBrampton";

            if (listing.Year >= 2000 && !string.IsNullOrEmpty(listing.Title))
            {
                listings.Add(listing);
            }

            i++;
        }

        return listings;
    }
}
