namespace CarSearch.Providers.Platforms.D2cMedia;

public sealed record D2cMediaProviderDefinition(
    string Name,
    string DisplayName,
    string InventoryPath,
    string? DealerName = null);
