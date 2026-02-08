using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.TeamChrysler;

public class TeamChryslerSnapshotParser
{
    public string? ParseCity(string yaml)
    {
        return "Mississauga";
    }

    public int ParseResultCount(string yaml)
    {
        // Count listings by the short title link pattern (not the long image-description links)
        var pattern = @"link\s+""(?:19|20)\d{2}\s+[^""]+?""\s*\[ref=[^\]]+\]\s*\[cursor=pointer\]:\s*\n\s*-\s*/url:\s+/inventory/";
        return Regex.Matches(yaml, pattern).Count;
    }

    public List<VehicleListing> ParseListings(string yaml, string baseUrl)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        // Match the short title link: link "2014 Dodge Grand Caravan 30th Anniversary" [ref=...] [cursor=pointer]:
        // These are distinct from image description links which include "in Color for sale at"
        var titleLinkPattern = @"link\s+""((?:19|20)\d{2})\s+([^""]+)""\s*\[ref=([^\]]+)\]\s*\[cursor=pointer\]";
        var urlPattern = @"/url:\s+(/inventory/[^\s]+)";
        var mileagePattern = @"generic\s+\[ref=[^\]]+\]:\s+([\d,]+)\s+km";
        var pricePattern = @"text:\s+\$([\d,]+)";
        var transmissionPattern = @"generic\s+\[ref=[^\]]+\]:\s+(Automatic|Manual|CVT)";
        var enginePattern = @"generic\s+\[ref=[^\]]+\]:\s+(\d+\.\d+L\s+\d+Cyl)";

        int i = 0;
        while (i < lines.Length)
        {
            var titleMatch = Regex.Match(lines[i], titleLinkPattern);
            if (!titleMatch.Success) { i++; continue; }

            var year = int.Parse(titleMatch.Groups[1].Value);
            var titleText = titleMatch.Groups[2].Value.Trim();

            // Skip image-description links that include "for sale at"
            if (titleText.Contains(" for sale at ") || titleText.Contains(" for sale in ") ||
                titleText.Contains("for Sale |"))
            {
                i++;
                continue;
            }

            // Skip non-listing links
            if (titleText.Contains("View Details") || titleText.Contains("Carfax") ||
                titleText.Contains("Gallery"))
            {
                i++;
                continue;
            }

            // Look ahead for URL
            var blockEnd = Math.Min(i + 3, lines.Length);
            var urlBlock = string.Join('\n', lines[i..blockEnd]);
            var urlMatch = Regex.Match(urlBlock, urlPattern);
            if (!urlMatch.Success) { i++; continue; }

            // Now look further ahead for specs (mileage, price, transmission)
            var specsEnd = Math.Min(i + 80, lines.Length);
            var specsBlock = string.Join('\n', lines[i..specsEnd]);

            var listing = new VehicleListing
            {
                Year = year,
                Title = $"{year} {titleText}",
                Url = baseUrl.TrimEnd('/') + urlMatch.Groups[1].Value,
                Dealer = "Team Chrysler",
                Location = "Mississauga, ON",
                Source = "TeamChrysler"
            };

            var mileageMatch = Regex.Match(specsBlock, mileagePattern);
            if (mileageMatch.Success)
                listing.Mileage = mileageMatch.Groups[1].Value + " km";

            // Find all price matches and take the one without "/ bw"
            var priceMatches = Regex.Matches(specsBlock, pricePattern);
            foreach (Match pm in priceMatches)
            {
                var lineEnd = specsBlock.IndexOf('\n', pm.Index);
                var lineContent = lineEnd >= 0
                    ? specsBlock[pm.Index..lineEnd]
                    : specsBlock[pm.Index..];
                if (!lineContent.Contains("/ bw") && !lineContent.Contains("/bw"))
                {
                    listing.Price = "$" + pm.Groups[1].Value;
                    break;
                }
            }

            var transMatch = Regex.Match(specsBlock, transmissionPattern);
            if (transMatch.Success)
                listing.Transmission = transMatch.Groups[1].Value;

            var engineMatch = Regex.Match(specsBlock, enginePattern);
            if (engineMatch.Success)
                listing.FuelType = engineMatch.Groups[1].Value;

            if (listing.Year >= 2000)
                listings.Add(listing);

            i++;
        }

        return listings;
    }
}
