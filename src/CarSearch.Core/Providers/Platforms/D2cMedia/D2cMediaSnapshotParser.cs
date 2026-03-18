using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.Platforms.D2cMedia;

public class D2cMediaSnapshotParser : ID2cMediaSnapshotParser
{
    protected virtual string ResultCountPattern => @"button\s+""(?:Used|Pre-Owned)\s+vehicles?\s+(\d+)""";
    protected virtual string ListingLinkPattern => @"link\s+""((?:19|20)\d{2})\s+(.+?)\s+in\s+([^""]+)""\s*\[ref=([^\]]+)\]";
    protected virtual string ListingUrlPattern => @"/url:\s+(/(?:used|new)/\d{4}-[^\s]+\.html)";
    protected virtual string ListingSpecsPattern => @"""([\d,]+)\s+KM\.\s+([^,]+),\s+Ext:\s+([^,""]+)";
    protected virtual string ListingPricePattern => @"text:\s+([\d,]+)\s+\$";
    protected virtual int ListingLookaheadLines => 30;

    public virtual string? FindListItemRef(string yaml, string label)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(label)}[^""]*""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public virtual string? FindColorRef(string yaml, string color)
    {
        var pattern = $@"listitem\s+""{Regex.Escape(color)}""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public virtual string? ParseCity(string yaml)
    {
        var pattern = @"heading\s+""[^""]*for\s+sale\s+in\s+([^""]+)""\s*\[level=1\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public virtual int ParseResultCount(string yaml)
    {
        var match = Regex.Match(yaml, ResultCountPattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        var linkPattern = @"link\s+""(?:19|20)\d{2}\s+.+?\s+in\s+[^""]+""";
        return Regex.Matches(yaml, linkPattern).Count;
    }

    public virtual List<VehicleListing> ParseListings(string yaml, D2cMediaListingContext context)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var linkMatch = Regex.Match(lines[i], ListingLinkPattern);
            if (!linkMatch.Success)
            {
                continue;
            }

            var listing = new VehicleListing
            {
                Year = int.Parse(linkMatch.Groups[1].Value),
                Title = $"{linkMatch.Groups[1].Value} {linkMatch.Groups[2].Value}",
                Location = linkMatch.Groups[3].Value,
                Dealer = context.DealerName,
                Source = context.SourceName
            };

            var blockEnd = Math.Min(i + ListingLookaheadLines, lines.Length);
            var block = string.Join('\n', lines[i..blockEnd]);

            var urlMatch = Regex.Match(block, ListingUrlPattern);
            if (urlMatch.Success)
            {
                listing.Url = context.BaseUrl.TrimEnd('/') + urlMatch.Groups[1].Value;
            }

            var priceMatch = Regex.Match(block, ListingPricePattern);
            if (priceMatch.Success)
            {
                listing.Price = "$" + priceMatch.Groups[1].Value;
            }

            var specsMatch = Regex.Match(block, ListingSpecsPattern);
            if (specsMatch.Success)
            {
                listing.Mileage = specsMatch.Groups[1].Value + " km";
                listing.Transmission = specsMatch.Groups[2].Value.Trim();
            }

            if (listing.Year >= 2000)
            {
                listings.Add(listing);
            }
        }

        return listings;
    }
}
