using System.Diagnostics;
using System.Text;
using CarSearch.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Services;

public class PlaywrightCliService
{
    private readonly PlaywrightCliOptions _options;
    private readonly ILogger<PlaywrightCliService> _logger;

    public PlaywrightCliService(IOptions<PlaywrightCliOptions> options, ILogger<PlaywrightCliService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string args, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _options.DefaultTimeoutMs;
        _logger.LogDebug("Executing: {Command} {Args}", _options.Command, args);

        var psi = new ProcessStartInfo
        {
            FileName = _options.Command,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"playwright-cli timed out after {timeout}ms: {_options.Command} {args}");
        }

        var output = stdout.ToString().Trim();
        var error = stderr.ToString().Trim();

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("playwright-cli exited with code {ExitCode}: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"playwright-cli failed (exit code {process.ExitCode}): {error}");
        }

        _logger.LogDebug("Output: {Output}", output.Length > 200 ? output[..200] + "..." : output);
        return output;
    }

    public async Task OpenAsync(string? url = null)
    {
        var args = url != null ? $"open {url}" : "open";
        await ExecuteAsync(args);
    }

    public async Task<string> SnapshotAsync()
    {
        var output = await ExecuteAsync("snapshot");

        // The snapshot command outputs the path to the YAML file
        // Formats seen: bare path "C:\...\snapshot.yml" or markdown link "[Snapshot](C:\...\snapshot.yml)"
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? yamlPath = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check for markdown link format: [Snapshot](path.yml)
            var mdMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"\[.*?\]\((.+?\.ya?ml)\)");
            if (mdMatch.Success)
            {
                yamlPath = mdMatch.Groups[1].Value;
                break;
            }

            // Check for bare path ending in .yml/.yaml
            if (trimmed.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            {
                yamlPath = trimmed;
                break;
            }
        }

        if (yamlPath != null && File.Exists(yamlPath))
        {
            return await File.ReadAllTextAsync(yamlPath);
        }

        // If we can't find a file path, return the raw output (it may be inline)
        return output;
    }

    public async Task<string> SnapshotWithRetryAsync()
    {
        for (int i = 0; i < _options.RetryCount; i++)
        {
            try
            {
                var result = await SnapshotAsync();
                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }
            catch (Exception ex) when (i < _options.RetryCount - 1)
            {
                _logger.LogWarning("Snapshot attempt {Attempt} failed: {Error}", i + 1, ex.Message);
            }

            await Task.Delay(_options.RetryDelayMs);
        }

        throw new InvalidOperationException("Failed to take snapshot after retries");
    }

    public async Task ClickAsync(string refId)
    {
        await ExecuteAsync($"click {refId}");
    }

    public async Task FillAsync(string refId, string text)
    {
        await ExecuteAsync($"fill {refId} \"{text.Replace("\"", "\\\"")}\"");
    }

    public async Task SelectAsync(string refId, string value)
    {
        await ExecuteAsync($"select {refId} \"{value.Replace("\"", "\\\"")}\"");
    }

    public async Task CheckAsync(string refId)
    {
        await ExecuteAsync($"check {refId}");
    }

    public async Task TypeAsync(string text)
    {
        await ExecuteAsync($"type \"{text.Replace("\"", "\\\"")}\"");
    }

    public async Task PressAsync(string key)
    {
        await ExecuteAsync($"press {key}");
    }

    public async Task RunCodeAsync(string jsCode)
    {
        await ExecuteAsync($"run-code \"{jsCode.Replace("\"", "\\\"")}\"");
    }

    public async Task GotoAsync(string url)
    {
        await ExecuteAsync($"goto {url}");
    }

    public async Task CloseAsync()
    {
        try
        {
            await ExecuteAsync("close");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error closing browser: {Error}", ex.Message);
        }
    }

    public async Task WaitAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }
}
