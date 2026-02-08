using System.Text;
using CarSearch.Models;

namespace CarSearch.Services;

public class MarkdownReportGenerator
{
    public string Generate(List<ProviderSearchResult> results, SearchParameters parameters)
    {
        var sb = new StringBuilder();

        // Determine city from first successful provider
        var city = results.FirstOrDefault(r => r.Success && r.City != null)?.City;
        var locationDisplay = city ?? parameters.PostalCode;

        // Header
        var yearSuffix = parameters.YearFrom.HasValue ? $" ({parameters.YearFrom}+)" : "";
        var colorPrefix = parameters.Color != null ? $"{parameters.Color} " : "";
        sb.AppendLine($"# {colorPrefix}{parameters.Make} {parameters.Model} Listings - {locationDisplay}, Ontario{yearSuffix}");
        sb.AppendLine();
        sb.AppendLine($"**Search Date:** {DateTime.Now:MMMM d, yyyy}");

        var successfulSources = results.Where(r => r.Success).Select(r => r.DisplayName).ToList();
        sb.AppendLine($"**Sources:** {(successfulSources.Count > 0 ? string.Join(", ", successfulSources) : "None")}");

        var filterParts = new List<string> { $"{parameters.Make} {parameters.Model}" };
        if (parameters.Color != null) filterParts.Add(parameters.Color);
        if (parameters.YearFrom.HasValue) filterParts.Add($"{parameters.YearFrom}+");
        filterParts.Add($"100 km around {locationDisplay}, ON");
        sb.AppendLine($"**Filters:** {string.Join(" | ", filterParts)}");

        var totalResults = results.Where(r => r.Success).Sum(r => r.TotalCount);
        sb.AppendLine($"**Total Results:** {totalResults}");
        sb.AppendLine();

        // Provider summary table
        sb.AppendLine("## Provider Summary");
        sb.AppendLine();
        sb.AppendLine("| Source | Results | Duration | Status |");
        sb.AppendLine("|--------|---------|----------|--------|");
        foreach (var result in results)
        {
            var status = result.Success ? "OK" : $"Error: {result.ErrorMessage}";
            var count = result.Success ? result.Listings.Count.ToString() : "-";
            var duration = result.Duration.TotalSeconds > 0 ? $"{result.Duration.TotalSeconds:F1}s" : "-";
            sb.AppendLine($"| **{result.DisplayName}** | {count} | {duration} | {status} |");
        }
        sb.AppendLine();
        sb.AppendLine("---");

        // Listings grouped by provider
        foreach (var result in results.Where(r => r.Success && r.Listings.Count > 0))
        {
            sb.AppendLine();
            sb.AppendLine($"## {result.DisplayName} ({result.Listings.Count} listings)");
            sb.AppendLine();

            for (int i = 0; i < result.Listings.Count; i++)
            {
                var listing = result.Listings[i];
                sb.AppendLine($"### {i + 1}. {listing.Title}");
                sb.AppendLine();
                sb.AppendLine("| Detail | Value |");
                sb.AppendLine("|--------|-------|");
                sb.AppendLine($"| **URL** | [View Listing]({listing.Url}) |");

                // Price with MSRP annotation
                var priceDisplay = listing.Price;
                if (listing.IsNew)
                {
                    priceDisplay = $"{listing.Price} (MSRP)";
                }
                sb.AppendLine($"| **Price** | {priceDisplay} |");

                sb.AppendLine($"| **Mileage** | {listing.Mileage} |");
                sb.AppendLine($"| **Transmission** | {listing.Transmission} |");
                sb.AppendLine($"| **Fuel Type** | {listing.FuelType} |");

                if (listing.PriceRating != null)
                {
                    sb.AppendLine($"| **Price Rating** | {listing.PriceRating} |");
                }

                if (listing.IsNew)
                {
                    sb.AppendLine($"| **Condition** | New |");
                }

                sb.AppendLine($"| **Dealer** | {listing.Dealer} |");
                sb.AppendLine($"| **Location** | {listing.Location} |");
                sb.AppendLine($"| **Source** | {listing.Source} |");
                sb.AppendLine();
                sb.AppendLine("---");
            }
        }

        // Aggregated summary section
        var allListings = results.Where(r => r.Success).SelectMany(r => r.Listings).ToList();

        if (allListings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine();

            var newListings = allListings.Where(l => l.IsNew).ToList();
            var usedListings = allListings.Where(l => !l.IsNew).ToList();

            var allPrices = allListings.Select(ParsePrice).Where(p => p > 0).ToList();
            var newPrices = newListings.Select(ParsePrice).Where(p => p > 0).ToList();
            var usedPrices = usedListings.Select(ParsePrice).Where(p => p > 0).ToList();

            sb.AppendLine("| Category | Count | Price Range |");
            sb.AppendLine("|----------|-------|-------------|");

            if (newListings.Count > 0 && newPrices.Count > 0)
            {
                sb.AppendLine($"| **New vehicles** | {newListings.Count} | {FormatPrice(newPrices.Min())} - {FormatPrice(newPrices.Max())} |");
            }
            if (usedListings.Count > 0 && usedPrices.Count > 0)
            {
                sb.AppendLine($"| **Used vehicles** | {usedListings.Count} | {FormatPrice(usedPrices.Min())} - {FormatPrice(usedPrices.Max())} |");
            }
            if (allPrices.Count > 0)
            {
                sb.AppendLine($"| **Total listings** | {allListings.Count} | {FormatPrice(allPrices.Min())} - {FormatPrice(allPrices.Max())} |");
            }

            // Price breakdown for used only
            if (usedPrices.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Price Breakdown (Used Only)");

                var brackets = new (int min, int max, string label)[]
                {
                    (0, 75000, "Under $75,000"),
                    (75000, 100000, "$75,000 - $100,000"),
                    (100000, 135000, "$100,000 - $135,000"),
                    (135000, int.MaxValue, "$135,000+")
                };

                foreach (var (min, max, label) in brackets)
                {
                    var count = usedPrices.Count(p => p >= min && p < max);
                    if (count > 0)
                    {
                        sb.AppendLine($"- {label}: {count} {(count == 1 ? "listing" : "listings")}");
                    }
                }
            }

            // Year distribution
            var yearGroups = allListings.GroupBy(l => l.Year).OrderBy(g => g.Key).ToList();
            if (yearGroups.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Year Distribution");
                foreach (var group in yearGroups)
                {
                    sb.AppendLine($"- {group.Key}: {group.Count()} {(group.Count() == 1 ? "listing" : "listings")}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("*All prices exclude applicable taxes and licensing. Information was accurate at the time of search. Please contact the respective dealers for the most up-to-date pricing and availability.*");
        sb.AppendLine();

        return sb.ToString();
    }

    private static decimal ParsePrice(VehicleListing listing)
    {
        var priceStr = listing.Price.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(priceStr, out var price) ? price : 0;
    }

    private static string FormatPrice(decimal price)
    {
        return $"${price:N0}";
    }
}
