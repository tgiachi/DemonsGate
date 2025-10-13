using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Services;

/// <summary>
/// Service for managing game scenes, including loading, switching, and lifecycle management.
/// Supports scene transitions and maintains a collection of loaded scenes.
/// </summary>
public class SceneManager
{
    private readonly ILogger _logger = Log.ForContext<SceneManager>();
    private readonly Dictionary<string, IScene> _scenes = new();
    private ISceneTransition? _activeTransition;

    public SceneManager()
    {
        _logger.Information("SceneManager created");
    }

    /// <summary>
    /// Gets the currently active scene
    /// </summary>
    public IScene? CurrentScene { get; private set; }

    /// <summary>
    /// Gets all loaded scenes
    /// </summary>
    public IEnumerable<IScene> Scenes => _scenes.Values;

    /// <summary>
    /// Gets whether a transition is currently active
    /// </summary>
    public bool IsTransitioning => _activeTransition != null && !_activeTransition.IsCompleted;

    /// <summary>
    /// Registers a scene with the manager without loading it
    /// </summary>
    /// <param name="scene">The scene to register</param>
    public void RegisterScene(IScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (_scenes.TryAdd(scene.Name, scene))
        {
            _logger.Debug("Registered scene: {SceneName}", scene.Name);
        }
        else
        {
            _logger.Warning("Scene '{SceneName}' is already registered", scene.Name);
        }
    }

    /// <summary>
    /// Registers and loads a scene immediately
    /// </summary>
    /// <param name="scene">The scene to register and load</param>
    public void RegisterAndLoadScene(IScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        RegisterScene(scene);
        scene.Load();
        _logger.Debug("Registered and loaded scene: {SceneName}", scene.Name);
    }

    /// <summary>
    /// Loads a scene and makes it the current scene
    /// </summary>
    /// <param name="scene">The scene to load</param>
    public void LoadScene(IScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _logger.Information("Loading scene: {SceneName}", scene.Name);

        // Add to scenes collection if not already present
        _scenes.TryAdd(scene.Name, scene);

        // Unload current scene if exists
        if (CurrentScene != null)
        {
            _logger.Debug("Unloading current scene: {CurrentSceneName}", CurrentScene.Name);
            CurrentScene.Unload();
        }

        // Load and set new scene
        scene.Load();
        CurrentScene = scene;

        _logger.Information("Scene loaded successfully: {SceneName}", scene.Name);
    }

    /// <summary>
    /// Loads a scene by name and makes it the current scene
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    public void LoadScene(string sceneName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneName);

        if (!_scenes.TryGetValue(sceneName, out var scene))
        {
            _logger.Warning("Cannot load scene '{SceneName}' - scene not found", sceneName);
            throw new InvalidOperationException($"Scene '{sceneName}' is not loaded");
        }

        LoadScene(scene);
    }

    /// <summary>
    /// Switches to an already loaded scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene to switch to</param>
    public void SwitchToScene(string sceneName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneName);

        if (!_scenes.TryGetValue(sceneName, out var scene))
        {
            _logger.Warning("Cannot switch to scene '{SceneName}' - scene not found", sceneName);
            throw new InvalidOperationException($"Scene '{sceneName}' is not loaded");
        }

        _logger.Information("Switching to scene: {SceneName}", sceneName);

        // Unload current scene if exists
        if (CurrentScene != null)
        {
            _logger.Debug("Unloading current scene: {CurrentSceneName}", CurrentScene.Name);
            CurrentScene.Unload();
        }

        // Switch to new scene
        scene.Load();
        CurrentScene = scene;

        _logger.Information("Successfully switched to scene: {SceneName}", sceneName);
    }

    /// <summary>
    /// Switches to an already loaded scene by name with a transition
    /// </summary>
    /// <param name="sceneName">The name of the scene to switch to</param>
    /// <param name="transition">The transition to use</param>
    public void SwitchToScene(string sceneName, ISceneTransition transition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneName);
        ArgumentNullException.ThrowIfNull(transition);

        if (!_scenes.TryGetValue(sceneName, out var scene))
        {
            _logger.Warning("Cannot switch to scene '{SceneName}' - scene not found", sceneName);
            throw new InvalidOperationException($"Scene '{sceneName}' is not loaded");
        }

        SwitchToScene(scene, transition);
    }

    /// <summary>
    /// Switches to a scene with a transition
    /// </summary>
    /// <param name="scene">The scene to switch to</param>
    /// <param name="transition">The transition to use</param>
    public void SwitchToScene(IScene scene, ISceneTransition transition)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(transition);

        _logger.Information("Starting transition to scene: {SceneName}", scene.Name);

        // Add scene to collection if not already present
        _scenes.TryAdd(scene.Name, scene);

        // Dispose previous transition if exists
        _activeTransition?.Dispose();

        // Set up new transition
        _activeTransition = transition;
        _activeTransition.Completed += OnTransitionCompleted;

        // Load target scene
        scene.Load();

        // Start transition
        transition.Start(CurrentScene, scene);

        _logger.Debug(
            "Transition started from {FromScene} to {ToScene}",
            CurrentScene?.Name ?? "None",
            scene.Name
        );
    }

    /// <summary>
    /// Unloads a scene and removes it from the manager
    /// </summary>
    /// <param name="scene">The scene to unload</param>
    public void UnloadScene(IScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _logger.Information("Unloading scene: {SceneName}", scene.Name);

        // Cannot unload current scene
        if (CurrentScene == scene)
        {
            _logger.Warning("Cannot unload current scene: {SceneName}", scene.Name);
            throw new InvalidOperationException("Cannot unload the current scene. Switch to another scene first.");
        }

        // Remove from collection and unload
        if (_scenes.Remove(scene.Name))
        {
            scene.Unload();
            _logger.Information("Scene unloaded successfully: {SceneName}", scene.Name);
        }
        else
        {
            _logger.Warning("Scene '{SceneName}' was not found in loaded scenes", scene.Name);
        }
    }

    /// <summary>
    /// Unloads a scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene to unload</param>
    public void UnloadScene(string sceneName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneName);

        if (_scenes.TryGetValue(sceneName, out var scene))
        {
            UnloadScene(scene);
        }
        else
        {
            _logger.Warning("Cannot unload scene '{SceneName}' - scene not found", sceneName);
        }
    }

    /// <summary>
    /// Unloads all scenes
    /// </summary>
    public void UnloadAllScenes()
    {
        _logger.Information("Unloading all scenes");

        // Unload current scene
        if (CurrentScene != null)
        {
            CurrentScene.Unload();
            CurrentScene = null;
        }

        // Unload all other scenes
        foreach (var scene in _scenes.Values)
        {
            scene.Unload();
        }

        _scenes.Clear();

        // Dispose active transition
        _activeTransition?.Dispose();
        _activeTransition = null;

        _logger.Information("All scenes unloaded successfully");
    }

    /// <summary>
    /// Gets a scene by name
    /// </summary>
    /// <param name="sceneName">The name of the scene</param>
    /// <returns>The scene if found, null otherwise</returns>
    public IScene? GetScene(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName) ? null : _scenes.GetValueOrDefault(sceneName);
    }

    /// <summary>
    /// Checks if a scene with the specified name is loaded
    /// </summary>
    /// <param name="sceneName">The name of the scene</param>
    /// <returns>True if the scene is loaded</returns>
    public bool HasScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && _scenes.ContainsKey(sceneName);
    }

    /// <summary>
    /// Updates the current scene and any active transitions
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public void Update(GameTime gameTime)
    {
        // Update active transition first
        if (_activeTransition != null)
        {
            _activeTransition.Update(gameTime);

            // Don't update scenes during transition - let the transition handle it
            // The transition completion event will handle scene switching
        }
        else
        {
            // Update current scene if no transition is active
            CurrentScene?.Update(gameTime);
        }
    }

    /// <summary>
    /// Draws the current scene and any active transitions
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_activeTransition != null)
        {
            // Let transition handle drawing during transition
            _activeTransition.Draw(gameTime, spriteBatch);
        }
        else
        {
            // Draw current scene if no transition is active
            CurrentScene?.Draw(gameTime, spriteBatch);
        }
    }

    /// <summary>
    /// Handles transition completion
    /// </summary>
    private void OnTransitionCompleted(object? sender, EventArgs e)
    {
        if (_activeTransition?.ToScene == null)
        {
            return;
        }

        _logger.Information("Transition completed to scene: {SceneName}", _activeTransition.ToScene.Name);

        // Unload the previous scene
        if (CurrentScene != null && CurrentScene != _activeTransition.ToScene)
        {
            CurrentScene.Unload();
        }

        // Set new current scene
        CurrentScene = _activeTransition.ToScene;

        // Clean up transition
        _activeTransition.Completed -= OnTransitionCompleted;
        _activeTransition.Dispose();
        _activeTransition = null;

        _logger.Debug("Transition cleanup completed");
    }
}
