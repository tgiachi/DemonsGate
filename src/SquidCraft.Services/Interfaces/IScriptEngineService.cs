using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Services.Data.Scripts;

namespace SquidCraft.Services.Interfaces;

/// <summary>
///     Interface for the script engine service that manages JavaScript execution.
/// </summary>
public interface IScriptEngineService : ISquidCraftStartableService
{
    /// <summary>
    ///     Event raised when a script error occurs
    /// </summary>
    event EventHandler<ScriptErrorInfo>? OnScriptError;
    /// <summary>
    ///     Adds a script to be executed during engine initialization.
    /// </summary>
    /// <param name="script">The JavaScript code to execute on startup.</param>
    void AddInitScript(string script);

    /// <summary>
    ///     Executes a JavaScript script string.
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    void ExecuteScript(string script);

    /// <summary>
    ///     Executes a JavaScript file.
    /// </summary>
    /// <param name="scriptFile">The path to the JavaScript file to execute.</param>
    void ExecuteScriptFile(string scriptFile);

    /// <summary>
    ///     Adds a callback function that can be called from JavaScript.
    /// </summary>
    /// <param name="name">The name of the callback function in JavaScript.</param>
    /// <param name="callback">The C# action to execute when the callback is invoked.</param>
    void AddCallback(string name, Action<object[]> callback);

    /// <summary>
    ///     Adds a constant value accessible from JavaScript.
    /// </summary>
    /// <param name="name">The name of the constant in JavaScript.</param>
    /// <param name="value">The value of the constant.</param>
    void AddConstant(string name, object value);

    /// <summary>
    ///     Executes a previously registered callback function.
    /// </summary>
    /// <param name="name">The name of the callback to execute.</param>
    /// <param name="args">Arguments to pass to the callback.</param>
    void ExecuteCallback(string name, params object[] args);

    /// <summary>
    ///     Adds a .NET type as a module accessible from JavaScript.
    /// </summary>
    /// <param name="type">The type to register as a script module.</param>
    void AddScriptModule(Type type);

    /// <summary>
    ///     Converts a .NET method name to a JavaScript-compatible function name.
    /// </summary>
    /// <param name="name">The .NET method name to convert.</param>
    /// <returns>The JavaScript-compatible function name.</returns>
    string ToScriptEngineFunctionName(string name);

    /// <summary>
    ///     Executes a JavaScript function and returns the result.
    /// </summary>
    /// <param name="command">The JavaScript function call to execute.</param>
    /// <returns>A ScriptResult containing the execution outcome.</returns>
    ScriptResult ExecuteFunction(string command);

    /// <summary>
    ///     Asynchronously executes a JavaScript function and returns the result.
    /// </summary>
    /// <param name="command">The JavaScript function call to execute.</param>
    /// <returns>A task containing a ScriptResult with the execution outcome.</returns>
    Task<ScriptResult> ExecuteFunctionAsync(string command);

    /// <summary>
    ///     Gets execution metrics for performance monitoring
    /// </summary>
    /// <returns>Metrics about script execution</returns>
    ScriptExecutionMetrics GetExecutionMetrics();

    /// <summary>
    ///     Clears the script cache
    /// </summary>
    void ClearScriptCache();

    /// <summary>
    ///     Gets the underlying script engine instance.
    ///     This is exposed as object to avoid tight coupling to specific engine implementations.
    /// </summary>
    object Engine { get; }
}
