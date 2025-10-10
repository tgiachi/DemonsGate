using System.Numerics;
using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Game.Data.Primitives;

namespace DemonsGate.Services.Game.Interfaces;

public interface IChunkGeneratorService : IDemonsGateService
{
    Task<ChunkEntity> GetChunk(Vector3 position);

}
