using SquidCraft.Services.Game.Interfaces.Pipeline;
using SquidCraft.Services.Interfaces;
using Serilog;

namespace SquidCraft.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generator step that executes a Lua script to modify the chunk.
/// </summary>
public class LuaGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<LuaGeneratorStep>();
    private readonly IScriptEngineService _scriptEngine;
    private readonly string _scriptContent;
    private readonly string _stepName;

    /// <summary>
    /// Initializes a new instance of the <see cref="LuaGeneratorStep"/> class.
    /// </summary>
    /// <param name="scriptEngine">The Lua script engine service.</param>
    /// <param name="stepName">The name of this generation step.</param>
    /// <param name="scriptContent">The Lua script content to execute.</param>
    public LuaGeneratorStep(IScriptEngineService scriptEngine, string stepName, string scriptContent)
    {
        _scriptEngine = scriptEngine ?? throw new ArgumentNullException(nameof(scriptEngine));
        _stepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        _scriptContent = scriptContent ?? throw new ArgumentNullException(nameof(scriptContent));
    }

    /// <inheritdoc/>
    public string Name => _stepName;

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Executing Lua generator step: {StepName}", _stepName);

        try
        {
            // Make the context available to the Lua script
            var luaScript = _scriptEngine.Engine as MoonSharp.Interpreter.Script;
            if (luaScript == null)
            {
                throw new InvalidOperationException("Script engine is not a MoonSharp Lua script");
            }

            // Register the context as a global variable for the script
            luaScript.Globals["_generation_context"] = context;

            // Execute the Lua script
            _scriptEngine.ExecuteScript(_scriptContent);

            _logger.Debug("Lua generator step {StepName} completed successfully", _stepName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing Lua generator step: {StepName}", _stepName);
            throw;
        }

        return Task.CompletedTask;
    }
}
