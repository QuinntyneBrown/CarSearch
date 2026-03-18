using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.BmwEtobicoke;

public class BmwEtobicokeProvider : D2cMediaProviderBase<BmwEtobicokeSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("BmwEtobicoke", "BMW Etobicoke", "/used/search.html");

    public BmwEtobicokeProvider(
        BmwEtobicokeSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<BmwEtobicokeProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
