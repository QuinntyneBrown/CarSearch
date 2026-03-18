using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.GatewayChevrolet;

public class GatewayChevroletProvider : D2cMediaProviderBase<GatewayChevroletSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("GatewayChevrolet", "Gateway Chevrolet Brampton", "/inventory/used/", "Gateway Chevrolet");

    public GatewayChevroletProvider(
        GatewayChevroletSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<GatewayChevroletProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
