using SquidCraft.Services.Game.Data;
using SquidCraft.Services.Game.Generation.Noise;
using SquidCraft.Services.Game.Interfaces.Pipeline;
using Serilog;

namespace SquidCraft.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generates biome data using temperature, moisture, and elevation noise maps.
/// Uses the Whittaker biome classification model.
/// </summary>
public class BiomeGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<BiomeGeneratorStep>();

    /// <summary>
    /// Scale for temperature noise (lower = larger temperature regions).
    /// </summary>
    private const float TemperatureScale = 0.003f;

    /// <summary>
    /// Scale for moisture noise (lower = larger moisture regions).
    /// </summary>
    private const float MoistureScale = 0.004f;

    /// <summary>
    /// Scale for elevation noise (lower = larger elevation regions).
    /// </summary>
    private const float ElevationScale = 0.002f;

    /// <inheritdoc/>
    public string Name => "BiomeGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Generating biome data for chunk at {Position}", context.WorldPosition);

        var worldPos = context.WorldPosition;
        var seed = context.Seed;

        // Create separate noise generators for temperature, moisture, and elevation
        // Using different seeds derived from the main seed
        var temperatureNoise = CreateNoiseGenerator(seed + 1000, TemperatureScale);
        var moistureNoise = CreateNoiseGenerator(seed + 2000, MoistureScale);
        var elevationNoise = CreateNoiseGenerator(seed + 3000, ElevationScale);

        // Sample the noise at the chunk's center position
        float centerX = worldPos.X + 8; // ChunkSize / 2
        float centerZ = worldPos.Z + 8;

        // Get normalized values (0.0 to 1.0)
        float temperature = NormalizeNoise(temperatureNoise.GetNoise(centerX, centerZ));
        float moisture = NormalizeNoise(moistureNoise.GetNoise(centerX, centerZ));
        float elevation = NormalizeNoise(elevationNoise.GetNoise(centerX, centerZ));

        // Determine biome type using Whittaker classification
        var biomeType = BiomeData.DetermineBiome(elevation, temperature, moisture);
        var biomeConfig = BiomeData.GetBiomeConfiguration(biomeType);

        // Create biome data
        var biomeData = new BiomeData
        {
            BiomeType = biomeType,
            Temperature = temperature,
            Moisture = moisture,
            Elevation = elevation,
            SurfaceBlock = biomeConfig.SurfaceBlock,
            SubsurfaceBlock = biomeConfig.SubsurfaceBlock,
            HeightMultiplier = biomeConfig.HeightMultiplier,
            BaseHeight = biomeConfig.BaseHeight
        };

        // Store biome data in context for other steps to use
        context.CustomData["BiomeData"] = biomeData;

        _logger.Debug(
            "Biome generated: {BiomeType} (Temp: {Temp:F2}, Moisture: {Moisture:F2}, Elevation: {Elevation:F2})",
            biomeType,
            temperature,
            moisture,
            elevation
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a noise generator with the specified seed and frequency.
    /// </summary>
    private static FastNoiseLite CreateNoiseGenerator(int seed, float scale)
    {
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(scale);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(4);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        return noise;
    }

    /// <summary>
    /// Normalizes noise value from [-1, 1] to [0, 1].
    /// </summary>
    private static float NormalizeNoise(float noiseValue)
    {
        return (noiseValue + 1f) * 0.5f;
    }
}
