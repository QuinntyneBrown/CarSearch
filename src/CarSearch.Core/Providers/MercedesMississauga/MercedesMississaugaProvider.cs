using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.MercedesMississauga;

public class MercedesMississaugaProvider : D2cMediaProviderBase<MercedesMississaugaSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("MercedesMississauga", "Mercedes-Benz Mississauga", "/used/search.html");

    public MercedesMississaugaProvider(
        MercedesMississaugaSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<MercedesMississaugaProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
