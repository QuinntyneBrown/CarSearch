using System.Text.RegularExpressions;
using CarSearch.Models;

namespace CarSearch.Providers.AutoTrader;

public class AutoTraderSnapshotParser
{
    /// <summary>
    /// Find a combobox ref by its name/label attribute.
    /// e.g. combobox "cars-make-filter" [ref=e73]
    /// </summary>
    public string? FindComboboxRef(string yaml, string name)
    {
        var pattern = $@"combobox\s+""{Regex.Escape(name)}[^""]*""\s+.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a button ref by its label text.
    /// e.g. button "Exterior color filter" [ref=e117]
    /// </summary>
    public string? FindButtonRef(string yaml, string label)
    {
        var pattern = $@"button\s+""{Regex.Escape(label)}[^""]*""\s+.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a checkbox ref by matching its label text (partial match).
    /// e.g. checkbox "White (13)" [ref=e456] or generic with checkbox role
    /// The color filter uses checkboxes like: checkbox "White (13)"
    /// </summary>
    public string? FindCheckboxRef(string yaml, string label)
    {
        // Look for a checkbox element whose label starts with the given text
        var pattern = $@"checkbox\s+""{Regex.Escape(label)}[^""]*""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find an option ref by its text content.
    /// e.g. option "2021"
    /// </summary>
    public string? FindOptionRef(string yaml, string text)
    {
        var pattern = $@"option\s+""{Regex.Escape(text)}""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a link ref by matching its text (regex pattern).
    /// e.g. link "353,692 results" [ref=e88]
    /// </summary>
    public string? FindLinkRef(string yaml, string textPattern)
    {
        var pattern = $@"link\s+""({textPattern})""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[2].Value : null;
    }

    /// <summary>
    /// Find the "See N results" or "N results" link ref.
    /// </summary>
    public string? FindResultsLinkRef(string yaml)
    {
        // Match patterns like "353,692 results" or "13 results for..."
        var pattern = @"link\s+""[\d,]+\s+results[^""]*""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find the "Apply" or "See N results" button in a filter panel.
    /// </summary>
    public string? FindApplyButtonRef(string yaml)
    {
        // Look for button with "See" and "results" or "Apply"
        var pattern = @"button\s+""(?:See\s+[\d,]+\s+results|Apply)[^""]*""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a combobox ref that contains specific text in its label.
    /// Used for finding the model autocomplete and postal code inputs.
    /// </summary>
    public string? FindComboboxRefContaining(string yaml, string partialName)
    {
        var pattern = $@"combobox\s+""[^""]*{Regex.Escape(partialName)}[^""]*""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a listbox option ref by text.
    /// e.g. in autocomplete suggestions: option "Range Rover" [ref=xxx]
    /// </summary>
    public string? FindListboxOptionRef(string yaml, string text)
    {
        var pattern = $@"option\s+""{Regex.Escape(text)}""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find a generic element ref by its text content.
    /// e.g. generic [ref=e211]: "20" for the display count
    /// </summary>
    public string? FindGenericWithText(string yaml, string text)
    {
        var pattern = $@"generic\s+\[ref=([^\]]+)\]:\s+""{Regex.Escape(text)}""";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Find the Display combobox ref.
    /// e.g. combobox "Display: 20" [ref=e212]
    /// </summary>
    public string? FindDisplayComboboxRef(string yaml)
    {
        var pattern = @"combobox\s+""Display[^""]*""\s*\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Parse the result count from the heading.
    /// e.g. heading "13 results for Land Rover Range Rover" [level=1]
    /// </summary>
    public int ParseResultCount(string yaml)
    {
        var pattern = @"heading\s+""([\d,]+)\s+results?\s+for";
        var match = Regex.Match(yaml, pattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value.Replace(",", ""));
        }
        return 0;
    }

    /// <summary>
    /// Parse the location from filter badges or alert text.
    /// e.g. button "100 km around L5b1c2 Mississauga, ON"
    /// or alert: "Land Rover for sale in L5b1c2 Mississauga, ON"
    /// </summary>
    public string? ParseCity(string yaml)
    {
        // Try to extract city from the location badge
        var pattern = @"(?:km\s+around|sale\s+in)\s+\S+\s+([^,""]+),\s*(\w+)";
        var match = Regex.Match(yaml, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }

    /// <summary>
    /// Parse all vehicle listings from article blocks in the snapshot.
    /// Each listing is an article element containing heading, price, table cells, and dealer info.
    /// </summary>
    public List<VehicleListing> ParseListings(string yaml)
    {
        var listings = new List<VehicleListing>();
        var lines = yaml.Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];

            // Look for article [ref=...] markers
            if (!Regex.IsMatch(line, @"^\s*-?\s*article\s+\[ref="))
            {
                i++;
                continue;
            }

            // Determine the indentation level of this article
            var articleIndent = GetIndentLevel(line);
            var listing = new VehicleListing();
            i++;

            // Collect all lines belonging to this article (lines with greater indent)
            var articleLines = new List<string>();
            while (i < lines.Length && GetIndentLevel(lines[i]) > articleIndent)
            {
                articleLines.Add(lines[i]);
                i++;
            }

            var articleBlock = string.Join('\n', articleLines);

            // Extract title from heading [level=2]
            var titleMatch = Regex.Match(articleBlock, @"heading\s+""([^""]+)""\s+\[level=2\]");
            if (titleMatch.Success)
            {
                listing.Title = titleMatch.Groups[1].Value;
            }

            // Extract year from title
            var yearMatch = Regex.Match(listing.Title, @"^(\d{4})\s+");
            if (yearMatch.Success)
            {
                listing.Year = int.Parse(yearMatch.Groups[1].Value);
            }

            // Extract URL from the link preceding/containing the heading
            // Look for /url: pattern after a link that contains the heading text
            var urlMatches = Regex.Matches(articleBlock, @"- /url:\s+(https://www\.autotrader\.ca/offers/[^\s?]+)");
            if (urlMatches.Count > 0)
            {
                // Use the cleanest URL (without query params) — prefer the second match
                // as the first link usually has query params
                foreach (Match urlMatch in urlMatches)
                {
                    listing.Url = urlMatch.Groups[1].Value;
                    break;
                }
                // Try to find a clean URL (2nd or later matches tend to be cleaner)
                if (urlMatches.Count > 1)
                {
                    listing.Url = urlMatches[1].Groups[1].Value;
                }
            }

            // Extract price from paragraph: $XX,XXX
            var priceMatch = Regex.Match(articleBlock, @"paragraph\s+\[ref=[^\]]+\]:\s+(\$[\d,]+)");
            if (priceMatch.Success)
            {
                listing.Price = priceMatch.Groups[1].Value;
            }

            // Check for "New vehicle" indicator
            var newMatch = Regex.Match(articleBlock, @"generic\s+""New\s+vehicle""");
            if (newMatch.Success)
            {
                listing.IsNew = true;
            }

            // Extract MSRP price if present
            var msrpMatch = Regex.Match(articleBlock, @"paragraph\s+\[ref=[^\]]+\]:\s+(\$[\d,]+)\s+MSRP");
            if (msrpMatch.Success)
            {
                listing.MsrpPrice = msrpMatch.Groups[1].Value;
            }

            // Extract price rating (Great price, Good price, etc.)
            var ratingMatch = Regex.Match(articleBlock, @"generic\s+""(Great|Good|Fair|High)\s+price""");
            if (ratingMatch.Success)
            {
                listing.PriceRating = ratingMatch.Groups[1].Value + " price";
            }

            // Extract mileage, transmission, fuel type from table cells
            var cellMatches = Regex.Matches(articleBlock, @"cell\s+""([^""]*)""\s+\[ref=");
            if (cellMatches.Count >= 1)
            {
                listing.Mileage = cellMatches[0].Groups[1].Value;
            }
            if (cellMatches.Count >= 2)
            {
                listing.Transmission = cellMatches[1].Groups[1].Value;
            }
            if (cellMatches.Count >= 3)
            {
                listing.FuelType = cellMatches[2].Groups[1].Value;
            }

            // Handle cells with no text (e.g. transmission "n/a")
            // cell [ref=e3183] without quoted text means n/a
            if (string.IsNullOrEmpty(listing.Transmission))
            {
                var cellNoTextMatch = Regex.Matches(articleBlock, @"cell\s+\[ref=");
                if (cellNoTextMatch.Count > 0)
                {
                    // Check for n/a text nearby
                    var naMatch = Regex.Match(articleBlock, @"cell\s+\[ref=[^\]]+\]:\s*\n\s*.*?text:\s+n/a");
                    if (naMatch.Success)
                    {
                        listing.Transmission = "N/A";
                    }
                }
            }

            // Extract dealer and location from the footer generics
            // The last section of each article has:
            //   generic [ref=...]: Dealer Name
            //   generic [ref=...]: City, ON • XX km from you
            var footerPattern = @"generic\s+\[ref=[^\]]+\]:\s+(.+)";
            var footerMatches = Regex.Matches(articleBlock, footerPattern);
            if (footerMatches.Count >= 2)
            {
                // Last two generic text elements are dealer and location
                listing.Dealer = footerMatches[^2].Groups[1].Value.Trim();
                listing.Location = footerMatches[^1].Groups[1].Value.Trim();
            }

            // Only add if we have a title (skip "Explore similar vehicles" etc.)
            if (!string.IsNullOrEmpty(listing.Title) && listing.Year >= 2000)
            {
                listing.Source = "AutoTrader";
                listings.Add(listing);
            }
        }

        return listings;
    }

    public string? FindFirstListboxOption(string yaml)
    {
        // Find first option in a listbox "Suggestions"
        var pattern = @"listbox\s+""Suggestions"".*?\n\s*-\s*option\s+""[^""]*""\s*.*?\[ref=([^\]]+)\]";
        var match = Regex.Match(yaml, pattern, RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        // Fallback: find any option with [selected] in a listbox
        var pattern2 = @"option\s+""[^""]*""\s*\[selected\]\s*\[ref=([^\]]+)\]";
        match = Regex.Match(yaml, pattern2);
        if (match.Success) return match.Groups[1].Value;

        return null;
    }

    private static int GetIndentLevel(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == ' ') count++;
            else break;
        }
        return count;
    }
}
