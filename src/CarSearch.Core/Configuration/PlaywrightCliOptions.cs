namespace CarSearch.Configuration;

public class PlaywrightCliOptions
{
    public string Command { get; set; } = "playwright-cli";
    public int DefaultTimeoutMs { get; set; } = 15000;
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 2000;
}
