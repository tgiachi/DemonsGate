using System.Numerics;
using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Game.Data.Primitives;

namespace DemonsGate.Services.Game.Interfaces;

public interface IChunkGeneratorService : IDemonsGateService
{

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

}
