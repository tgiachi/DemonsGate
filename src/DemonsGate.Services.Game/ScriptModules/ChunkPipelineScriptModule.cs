using DemonsGate.Core.Attributes.Scripts;
using DemonsGate.Services.Game.Impl;
using Serilog;

namespace DemonsGate.Services.Game.ScriptModules;

/// <summary>
/// Lua script module that allows managing the chunk generation pipeline from Lua scripts.
/// </summary>
[ScriptModule("chunk_pipeline")]
public class ChunkPipelineScriptModule
{
    private readonly ILogger _logger = Log.ForContext<ChunkPipelineScriptModule>();
    private readonly ChunkGeneratorService _chunkGeneratorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkPipelineScriptModule"/> class.
    /// </summary>
    public ChunkPipelineScriptModule(ChunkGeneratorService chunkGeneratorService)
    {
        _chunkGeneratorService = chunkGeneratorService ?? throw new ArgumentNullException(nameof(chunkGeneratorService));
    }


    /// <summary>
    /// Removes a generator step from the pipeline by name.
    /// </summary>
    /// <param name="stepName">The name of the step to remove.</param>
    [ScriptFunction("remove_step")]
    public bool RemoveStep(string stepName)
    {
        try
        {
            var result = _chunkGeneratorService.RemoveGeneratorStep(stepName);

            if (result)
            {
                _logger.Information("Removed generator step '{StepName}' from pipeline", stepName);
            }
            else
            {
                _logger.Warning("Generator step '{StepName}' not found in pipeline", stepName);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove generator step '{StepName}'", stepName);
            throw;
        }
    }

    /// <summary>
    /// Gets the count of generator steps in the pipeline.
    /// </summary>
    [ScriptFunction("get_step_count")]
    public int GetStepCount()
    {
        return _chunkGeneratorService.GetGeneratorSteps().Count;
    }

    /// <summary>
    /// Gets the names of all generator steps in the pipeline.
    /// </summary>
    [ScriptFunction("get_step_names")]
    public object GetStepNames()
    {
        return _chunkGeneratorService.GetGeneratorSteps()
            .Select(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// Clears all generator steps from the pipeline.
    /// </summary>
    [ScriptFunction("clear_all_steps")]
    public void ClearAllSteps()
    {
        try
        {
            _chunkGeneratorService.ClearGeneratorSteps();
            _logger.Information("Cleared all generator steps from pipeline");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear generator steps");
            throw;
        }
    }

    /// <summary>
    /// Clears the chunk cache.
    /// </summary>
    [ScriptFunction("clear_cache")]
    public void ClearCache()
    {
        try
        {
            _chunkGeneratorService.ClearCache();
            _logger.Information("Cleared chunk cache");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear chunk cache");
            throw;
        }
    }

    /// <summary>
    /// Gets the number of cached chunks.
    /// </summary>
    [ScriptFunction("get_cached_chunk_count")]
    public int GetCachedChunkCount()
    {
        return _chunkGeneratorService.CachedChunkCount;
    }
}
