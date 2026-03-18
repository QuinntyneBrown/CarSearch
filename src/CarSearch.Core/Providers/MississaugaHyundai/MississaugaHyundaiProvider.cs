using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.MississaugaHyundai;

public class MississaugaHyundaiProvider : D2cMediaProviderBase<MississaugaHyundaiSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("MississaugaHyundai", "Mississauga Hyundai", "/used/");

    public MississaugaHyundaiProvider(
        MississaugaHyundaiSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<MississaugaHyundaiProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
