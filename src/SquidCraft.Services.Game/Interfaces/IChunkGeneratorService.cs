using System.Numerics;
using SquidCraft.Core.Interfaces.Metrics;
using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Services.Game.Interfaces.Pipeline;

namespace SquidCraft.Services.Game.Interfaces;

public interface IChunkGeneratorService : ISquidCraftStartableService, IMetricsProvider
{

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    Task GenerateInitialChunksAsync();

    void AddGeneratorStep(IGeneratorStep step);

    void ClearCache();

    bool RemoveGeneratorStep(string stepName);

}
