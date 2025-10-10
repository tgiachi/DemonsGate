using System.Numerics;
using DemonsGate.Core.Interfaces.Metrics;
using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Game.Interfaces.Pipeline;

namespace DemonsGate.Services.Game.Interfaces;

public interface IChunkGeneratorService : IDemonsGateStartableService, IMetricsProvider
{

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    Task GenerateInitialChunksAsync();

    void AddGeneratorStep(IGeneratorStep step);

    void ClearCache();

    bool RemoveGeneratorStep(string stepName);

}
