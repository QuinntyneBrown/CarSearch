using System.Diagnostics;
using System.Text;
using CarSearch.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Services;

public class PlaywrightCliService
{
    private readonly IOptionsMonitor<PlaywrightCliOptions> _optionsMonitor;
    private readonly ILogger<PlaywrightCliService> _logger;
    private readonly int? _sessionTimeoutMs;
    private readonly CancellationToken _sessionCancellationToken;

    public PlaywrightCliService(
        IOptionsMonitor<PlaywrightCliOptions> optionsMonitor,
        ILogger<PlaywrightCliService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    private PlaywrightCliService(
        IOptionsMonitor<PlaywrightCliOptions> optionsMonitor,
        ILogger<PlaywrightCliService> logger,
        int? sessionTimeoutMs,
        CancellationToken sessionCancellationToken)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _sessionTimeoutMs = sessionTimeoutMs;
        _sessionCancellationToken = sessionCancellationToken;
    }

    public PlaywrightCliService CreateSession(int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        return new PlaywrightCliService(_optionsMonitor, _logger, timeoutMs, cancellationToken);
    }

    public async Task<string> ExecuteAsync(string args, int? timeoutMs = null, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        var timeout = timeoutMs ?? _sessionTimeoutMs ?? options.DefaultTimeoutMs;
        _logger.LogDebug("Executing: {Command} {Args}", options.Command, args);

        var psi = new ProcessStartInfo
        {
            FileName = options.Command,
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

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _sessionCancellationToken,
            cancellationToken,
            timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                 !_sessionCancellationToken.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"playwright-cli timed out after {timeout}ms: {options.Command} {args}");
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
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

    public async Task OpenAsync(string? url = null, CancellationToken cancellationToken = default)
    {
        var args = url != null ? $"open {url}" : "open";
        await ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    public async Task<string> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        var output = await ExecuteAsync("snapshot", cancellationToken: cancellationToken);

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
            return await File.ReadAllTextAsync(yamlPath, cancellationToken);
        }

        // If we can't find a file path, return the raw output (it may be inline)
        return output;
    }

    public async Task<string> SnapshotWithRetryAsync(CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        for (int i = 0; i < options.RetryCount; i++)
        {
            try
            {
                var result = await SnapshotAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }
            catch (OperationCanceledException) when (_sessionCancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (i < options.RetryCount - 1)
            {
                _logger.LogWarning("Snapshot attempt {Attempt} failed: {Error}", i + 1, ex.Message);
            }

            await WaitInternalAsync(options.RetryDelayMs, cancellationToken);
        }

        throw new InvalidOperationException("Failed to take snapshot after retries");
    }

    public async Task ClickAsync(string refId, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync($"click {refId}", cancellationToken: cancellationToken);
    }

    public async Task FillAsync(string refId, string text, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            $"fill {refId} \"{text.Replace("\"", "\\\"")}\"",
            cancellationToken: cancellationToken);
    }

    public async Task SelectAsync(string refId, string value, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            $"select {refId} \"{value.Replace("\"", "\\\"")}\"",
            cancellationToken: cancellationToken);
    }

    public async Task CheckAsync(string refId, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync($"check {refId}", cancellationToken: cancellationToken);
    }

    public async Task TypeAsync(string text, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            $"type \"{text.Replace("\"", "\\\"")}\"",
            cancellationToken: cancellationToken);
    }

    public async Task PressAsync(string key, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync($"press {key}", cancellationToken: cancellationToken);
    }

    public async Task RunCodeAsync(string jsCode, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            $"run-code \"{jsCode.Replace("\"", "\\\"")}\"",
            cancellationToken: cancellationToken);
    }

    public async Task GotoAsync(string url, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync($"goto {url}", cancellationToken: cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync("close", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error closing browser: {Error}", ex.Message);
        }
    }

    public Task WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
    {
        return WaitInternalAsync(milliseconds, cancellationToken);
    }

    private async Task WaitInternalAsync(int milliseconds, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _sessionCancellationToken,
            cancellationToken);
        await Task.Delay(milliseconds, linkedCts.Token);
    }
}
