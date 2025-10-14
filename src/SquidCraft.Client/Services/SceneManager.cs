using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.Interfaces;
using SquidCraft.Client.Interfaces.Services;

namespace SquidCraft.Client.Services;

/// <summary>
/// Service for managing game scenes, including loading, switching, and lifecycle management.
/// Supports scene transitions and maintains a collection of loaded scenes.
/// </summary>
public class SceneManager : ISceneManager, ISCDrawableComponent, IParentAwareComponent
{
    private readonly ILogger _logger = Log.ForContext<SceneManager>();
    private readonly Dictionary<string, IScene> _scenes = new();
    private ISceneTransition? _activeTransition;
    private ISCDrawableComponent? _parent;

    public SceneManager()
    {
        _logger.Information("SceneManager created");
    }

    #region ISCDrawableComponent Implementation

    /// <summary>
    /// Gets the unique identifier of the SceneManager
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the SceneManager
    /// </summary>
    public string Name { get; set; } = nameof(SceneManager);

    /// <summary>
    /// Gets or sets the position (not used for SceneManager)
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets the size (not used for SceneManager)
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the scale (not used for SceneManager)
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets the parent component (SceneManager has no parent)
    /// </summary>
    public ISCDrawableComponent? Parent => _parent;

    /// <summary>
    /// Gets the children components (scenes are not exposed as children)
    /// </summary>
    public IEnumerable<ISCDrawableComponent> Children => Enumerable.Empty<ISCDrawableComponent>();

    /// <summary>
    /// Gets or sets the Z-index (not used for SceneManager)
    /// </summary>
    public int ZIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether the SceneManager is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the SceneManager is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the opacity (not used for SceneManager)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the rotation (not used for SceneManager)
    /// </summary>
    public float Rotation { get; set; } = 0f;

    /// <summary>
    /// Gets or sets whether the SceneManager has focus
    /// </summary>
    public bool IsFocused { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this SceneManager has input focus for keyboard and mouse events
    /// </summary>
    public bool HasFocus { get; set; } = true;

    #endregion

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


    public void LoadScene<TScene>() where TScene : IScene, new()
    {
        if (_scenes.TryGetValue(typeof(TScene).Name, out IScene scene))
        {
            LoadScene(scene.Name);

            return;
        }

        var newScene = new TScene();
        RegisterScene(newScene);
        LoadScene(newScene);
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
        if (!IsEnabled)
        {
            return;
        }

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
        if (!IsVisible)
        {
            return;
        }

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
    /// Handles keyboard input and propagates to current scene
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        // Propagate input to current scene if it has focus
        if (CurrentScene != null && CurrentScene.HasFocus)
        {
            CurrentScene.HandleKeyboard(keyboardState, gameTime);
        }
    }

    /// <summary>
    /// Handles mouse input and propagates to current scene
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        // Propagate input to current scene if it has focus
        if (CurrentScene != null && CurrentScene.HasFocus)
        {
            CurrentScene.HandleMouse(mouseState, gameTime);
        }
    }

    /// <summary>
    /// Checks if a point is within the SceneManager bounds (always returns false as SceneManager doesn't have bounds)
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>Always returns false</returns>
    public bool Contains(Vector2 point)
    {
        return false;
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

    void IParentAwareComponent.SetParent(ISCDrawableComponent? parent)
    {
        _parent = parent;
    }
}
