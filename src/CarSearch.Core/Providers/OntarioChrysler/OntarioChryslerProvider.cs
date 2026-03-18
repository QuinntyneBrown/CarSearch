using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.OntarioChrysler;

public class OntarioChryslerProvider : D2cMediaProviderBase<OntarioChryslerSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("OntarioChrysler", "Ontario Chrysler Mississauga", "/inventory/used/", "Ontario Chrysler");

    public OntarioChryslerProvider(
        OntarioChryslerSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<OntarioChryslerProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
