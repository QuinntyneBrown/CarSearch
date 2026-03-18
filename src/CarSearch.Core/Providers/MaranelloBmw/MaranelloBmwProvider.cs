using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.MaranelloBmw;

public class MaranelloBmwProvider : D2cMediaProviderBase<MaranelloBmwSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("MaranelloBmw", "Maranello BMW Vaughan", "/used/search.html", "Maranello BMW");

    public MaranelloBmwProvider(
        MaranelloBmwSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<MaranelloBmwProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
