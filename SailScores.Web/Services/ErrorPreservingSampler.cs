using OpenTelemetry.Trace;

namespace SailScores.Web.Services;

/// <summary>
/// Adaptive sampler for OpenTelemetry that maintains a target trace rate while preserving error telemetry.
/// 
/// Sampling Strategy:
/// - Always captures: 5xx Server Errors, 4xx Client Errors
/// - Blocks security scan paths: *.php, *.asp, *.jsp, *.cgi, robots.txt, sitemap.xml, .git, .env
/// - Adaptive sampling for 2xx successful responses: maintains target traces per minute with dynamic probability
/// 
/// This ensures:
/// 1. All errors are captured (no loss of critical information)
/// 2. Consistent logging volume regardless of traffic spikes
/// 3. Security scans are filtered out before transmission
/// 
/// The adaptive algorithm tracks traces in a sliding window and adjusts sampling probability
/// to maintain the configured target trace rate (e.g., 3000 traces/minute).
/// </summary>
public class AdaptiveErrorPreservingSampler : Sampler
{
    private readonly double _targetTracesPerSecond;
    private readonly int _windowSeconds;
    private readonly object _lock = new();
    private int _currentTraces;
    private DateTime _windowStart;
    private readonly HashSet<string> _blockedPaths = new();

    /// <summary>
    /// Initialize with a target trace rate for successful requests.
    /// </summary>
    /// <param name="targetTracesPerMinute">Target number of traces per minute to capture (e.g., 3000)</param>
    /// <param name="windowSeconds">Time window in seconds for rate calculation (default 60)</param>
    public AdaptiveErrorPreservingSampler(int targetTracesPerMinute = 3000, int windowSeconds = 60)
    {
        // Convert per-minute to per-second for internal calculations
        _targetTracesPerSecond = Math.Max(1, targetTracesPerMinute) / 60.0;
        _windowSeconds = Math.Max(1, windowSeconds);
        _currentTraces = 0;
        _windowStart = DateTime.UtcNow;

        // Paths to always drop (common security scans)
        _blockedPaths.Add(".php");
        _blockedPaths.Add(".asp");
        _blockedPaths.Add(".jsp");
        _blockedPaths.Add(".cgi");
        _blockedPaths.Add(".git");
        _blockedPaths.Add(".env");
    }

    /// <summary>
    /// Determine whether a span should be sampled based on HTTP status code, request path, and adaptive rate.
    /// </summary>
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Build a dictionary from tags for easier lookup
        var attributeDict = samplingParameters.Tags?.ToDictionary(x => x.Key, x => x.Value) ?? [];

        // Get HTTP status code if available
        int statusCode = 0;
        if (attributeDict.TryGetValue("http.status_code", out var statusObj) &&
            int.TryParse(statusObj?.ToString(), out int code))
        {
            statusCode = code;
        }

        // Always sample error responses (4xx, 5xx) - errors are critical and must be captured
        if (statusCode >= 400)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }

        // Get request path if available
        if (attributeDict.TryGetValue("http.target", out var pathObj))
        {
            var path = pathObj?.ToString() ?? "";

            // Drop security scan paths - these are noise and not useful
            if (IsBlockedPath(path))
            {
                return new SamplingResult(SamplingDecision.Drop);
            }
        }

        // Apply adaptive sampling for successful (2xx) requests
        // This maintains a consistent trace rate while respecting traffic patterns
        lock (_lock)
        {
            // Check if we need to reset the window
            var now = DateTime.UtcNow;
            if ((now - _windowStart).TotalSeconds >= _windowSeconds)
            {
                _currentTraces = 0;
                _windowStart = now;
            }

            // Calculate current sampling probability based on traces captured in this window
            // If we've already captured enough traces this window, reduce probability
            double targetPerSecond = _targetTracesPerSecond;
            double elapsedSeconds = (now - _windowStart).TotalSeconds;
            double expectedTracesAtThisPoint = targetPerSecond * elapsedSeconds;
            double samplingProbability = Math.Max(0.01, Math.Min(1.0, expectedTracesAtThisPoint / (_currentTraces + 1)));

            _currentTraces++;

            // Randomly decide whether to sample based on current probability
            if (Random.Shared.NextDouble() < samplingProbability)
            {
                return new SamplingResult(SamplingDecision.RecordAndSample);
            }
        }

        return new SamplingResult(SamplingDecision.Drop);
    }

    private bool IsBlockedPath(string path)
    {
        var lowerPath = path.ToLowerInvariant();
        return _blockedPaths.Any(blocked => lowerPath.Contains(blocked));
    }

    public new string Description => $"AdaptiveErrorPreservingSampler(target={_targetTracesPerSecond * 60:F0}/min, window={_windowSeconds}s)";
}
