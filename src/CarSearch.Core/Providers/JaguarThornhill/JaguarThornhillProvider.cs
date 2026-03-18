using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.JaguarThornhill;

public class JaguarThornhillProvider : D2cMediaProviderBase<JaguarThornhillSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("JaguarThornhill", "Jaguar Thornhill", "/used/search.html");

    public JaguarThornhillProvider(
        JaguarThornhillSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<JaguarThornhillProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
