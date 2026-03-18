using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.MississaugaHonda;

public class MississaugaHondaProvider : D2cMediaProviderBase<MississaugaHondaSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("MississaugaHonda", "Mississauga Honda", "/used/search.html");

    public MississaugaHondaProvider(
        MississaugaHondaSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<MississaugaHondaProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
