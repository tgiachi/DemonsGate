using DemonsGate.Services.Game.Interfaces.Pipeline;
using MoonSharp.Interpreter;
using Serilog;

namespace DemonsGate.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generator step that executes a Lua function to modify the chunk.
/// </summary>
public class LuaFunctionGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<LuaFunctionGeneratorStep>();
    private readonly DynValue _luaFunction;
    private readonly string _stepName;
    private readonly Script _luaScript;

    /// <summary>
    /// Initializes a new instance of the <see cref="LuaFunctionGeneratorStep"/> class.
    /// </summary>
    /// <param name="luaScript">The MoonSharp script instance.</param>
    /// <param name="stepName">The name of this generation step.</param>
    /// <param name="luaFunction">The Lua function to execute (must accept context parameter).</param>
    public LuaFunctionGeneratorStep(Script luaScript, string stepName, DynValue luaFunction)
    {
        _luaScript = luaScript ?? throw new ArgumentNullException(nameof(luaScript));
        _stepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        _luaFunction = luaFunction ?? throw new ArgumentNullException(nameof(luaFunction));

        if (_luaFunction.Type != DataType.Function)
        {
            throw new ArgumentException("Lua function parameter must be a function", nameof(luaFunction));
        }
    }

    /// <inheritdoc/>
    public string Name => _stepName;

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Executing Lua function generator step: {StepName}", _stepName);

        try
        {
            // Call the Lua function passing the context as parameter
            _luaScript.Call(_luaFunction, context);

            _logger.Debug("Lua function generator step {StepName} completed successfully", _stepName);
        }
        catch (ScriptRuntimeException luaEx)
        {
            _logger.Error(luaEx, "Lua error in generator step {StepName}: {Message}", _stepName, luaEx.DecoratedMessage);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing Lua function generator step: {StepName}", _stepName);
            throw;
        }

        return Task.CompletedTask;
    }
}
