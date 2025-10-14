using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Interfaces.Services;

/// <summary>
/// Interface for scene management service that handles scene loading, switching, and lifecycle management.
/// </summary>
public interface ISceneManager : ISCDrawableComponent
{
    /// <summary>
    /// Gets the currently active scene
    /// </summary>
    IScene? CurrentScene { get; }

    /// <summary>
    /// Gets all loaded scenes
    /// </summary>
    IEnumerable<IScene> Scenes { get; }

    /// <summary>
    /// Gets whether a transition is currently active
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// Registers a scene with the manager without loading it
    /// </summary>
    /// <param name="scene">The scene to register</param>
    void RegisterScene(IScene scene);

    /// <summary>
    /// Registers and loads a scene immediately
    /// </summary>
    /// <param name="scene">The scene to register and load</param>
    void RegisterAndLoadScene(IScene scene);

    /// <summary>
    /// Loads a scene by type and makes it the current scene
    /// </summary>
    /// <typeparam name="TScene">The type of scene to load</typeparam>
    void LoadScene<TScene>() where TScene : IScene, new();

    /// <summary>
    /// Loads a scene and makes it the current scene
    /// </summary>
    /// <param name="scene">The scene to load</param>
    void LoadScene(IScene scene);

    /// <summary>
    /// Loads a scene by name and makes it the current scene
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    void LoadScene(string sceneName);

    /// <summary>
    /// Switches to an already loaded scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene to switch to</param>
    void SwitchToScene(string sceneName);

    /// <summary>
    /// Switches to an already loaded scene by name with a transition
    /// </summary>
    /// <param name="sceneName">The name of the scene to switch to</param>
    /// <param name="transition">The transition to use</param>
    void SwitchToScene(string sceneName, ISceneTransition transition);

    /// <summary>
    /// Switches to a scene with a transition
    /// </summary>
    /// <param name="scene">The scene to switch to</param>
    /// <param name="transition">The transition to use</param>
    void SwitchToScene(IScene scene, ISceneTransition transition);

    /// <summary>
    /// Unloads a scene and removes it from the manager
    /// </summary>
    /// <param name="scene">The scene to unload</param>
    void UnloadScene(IScene scene);

    /// <summary>
    /// Unloads a scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene to unload</param>
    void UnloadScene(string sceneName);

    /// <summary>
    /// Unloads all scenes
    /// </summary>
    void UnloadAllScenes();

    /// <summary>
    /// Gets a scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene</param>
    /// <returns>The scene if found, null otherwise</returns>
    IScene? GetScene(string sceneName);

    /// <summary>
    /// Checks if a scene with the specified name is loaded
    /// </summary>
    /// <param name="sceneName">The name of the scene</param>
    /// <returns>True if the scene is loaded</returns>
    bool HasScene(string sceneName);

    /// <summary>
    /// Updates the current scene and any active transitions
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Draws the current scene and any active transitions
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
