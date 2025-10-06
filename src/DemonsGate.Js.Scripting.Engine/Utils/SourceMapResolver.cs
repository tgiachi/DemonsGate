using System.Text.Json;
using Serilog;

namespace Spectra.Scripting.Utils;

/// <summary>
///     Resolves source map information for TypeScript debugging
/// </summary>
public class SourceMapResolver
{
    private readonly Dictionary<string, SourceMapData> _sourceMaps = new();
    private readonly ILogger _logger = Log.ForContext<SourceMapResolver>();
    private readonly string _sourceMapsPath;

    public SourceMapResolver(string sourceMapsPath)
    {
        _sourceMapsPath = sourceMapsPath;
    }

    /// <summary>
    ///     Loads a source map file for a given JavaScript file
    /// </summary>
    public void LoadSourceMap(string jsFileName)
    {
        try
        {
            var sourceMapFile = Path.Combine(_sourceMapsPath, $"{jsFileName}.map");
            if (!File.Exists(sourceMapFile))
            {
                _logger.Debug("Source map not found for {FileName}", jsFileName);
                return;
            }

            var json = File.ReadAllText(sourceMapFile);
            var sourceMap = JsonSerializer.Deserialize<SourceMapData>(json);

            if (sourceMap != null)
            {
                _sourceMaps[jsFileName] = sourceMap;
                _logger.Debug("Source map loaded for {FileName}", jsFileName);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load source map for {FileName}", jsFileName);
        }
    }

    /// <summary>
    ///     Resolves original TypeScript location from compiled JavaScript location
    /// </summary>
    public (string? fileName, int? line, int? column) ResolveOriginalLocation(
        string jsFileName,
        int line,
        int column)
    {
        if (!_sourceMaps.TryGetValue(jsFileName, out var sourceMap))
        {
            return (null, null, null);
        }

        // Simplified source map resolution
        // In a real implementation, you'd decode the VLQ mappings
        // For now, return the first source file if available
        var originalFile = sourceMap.Sources?.FirstOrDefault();
        return (originalFile, line, column);
    }

    private class SourceMapData
    {
        public int Version { get; set; }
        public string? File { get; set; }
        public string? SourceRoot { get; set; }
        public List<string>? Sources { get; set; }
        public List<string>? Names { get; set; }
        public string? Mappings { get; set; }
    }
}
