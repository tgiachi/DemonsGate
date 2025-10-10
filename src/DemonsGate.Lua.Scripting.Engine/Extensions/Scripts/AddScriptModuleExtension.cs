using System;
using DemonsGate.Core.Data.Scripts;
using DemonsGate.Core.Extensions.Container;
using DryIoc;

namespace DemonsGate.Lua.Scripting.Engine.Extensions.Scripts;

/// <summary>
///     Extension methods for registering Lua script modules in the dependency injection container.
/// </summary>
public static class AddScriptModuleExtension
{
    /// <summary>
    ///     Registers a Lua script module type with the container.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    /// <param name="scriptModule">The type of the script module to register.</param>
    /// <returns>The container for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when scriptModule is null.</exception>
    public static IContainer AddLuaScriptModule(this IContainer container, Type scriptModule)
    {
        if (scriptModule == null)
        {
            throw new ArgumentNullException(nameof(scriptModule), "Script module type cannot be null.");
        }

        container.AddToRegisterTypedList(new ScriptModuleData(scriptModule));

        container.Register(scriptModule, Reuse.Singleton);

        return container;
    }

    /// <summary>
    ///     Registers a Lua script module type with the container using a generic type parameter.
    /// </summary>
    /// <typeparam name="TScriptModule">The type of the script module to register.</typeparam>
    /// <param name="container">The dependency injection container.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer AddLuaScriptModule<TScriptModule>(this IContainer container) where TScriptModule : class
    {
        return container.AddLuaScriptModule(typeof(TScriptModule));
    }
}
