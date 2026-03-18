using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.FourZeroOneDixieHyundai;

public class FourZeroOneDixieHyundaiProvider : D2cMediaProviderBase<FourZeroOneDixieHyundaiSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("FourZeroOneDixieHyundai", "401 Dixie Hyundai", "/used/");

    public FourZeroOneDixieHyundaiProvider(
        FourZeroOneDixieHyundaiSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<FourZeroOneDixieHyundaiProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
