namespace CarSearch.Configuration;

public class ProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = [];
}
