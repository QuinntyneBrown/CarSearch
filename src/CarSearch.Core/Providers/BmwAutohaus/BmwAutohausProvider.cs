using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.BmwAutohaus;

public class BmwAutohausProvider : D2cMediaProviderBase<BmwAutohausSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("BmwAutohaus", "BMW Autohaus Thornhill", "/used/search.html", "BMW Autohaus");

    public BmwAutohausProvider(
        BmwAutohausSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<BmwAutohausProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
