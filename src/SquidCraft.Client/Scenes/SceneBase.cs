using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Scenes;

/// <summary>
/// Abstract base class for scenes providing common functionality
/// </summary>
public abstract class SceneBase : IScene
{
    protected readonly ILogger Logger;

    protected SceneBase(string name)
    {
        Name = name;
        Logger = Log.ForContext(GetType());
    }

    /// <summary>
    /// Unique name of the scene
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the scene is currently loaded
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Gets or sets whether this scene has input focus for keyboard and mouse events
    /// </summary>
    public bool HasFocus { get; set; } = true;

    /// <summary>
    /// Loads the scene and initializes its resources
    /// </summary>
    public void Load()
    {
        if (IsLoaded)
        {
            Logger.Warning("Scene {SceneName} is already loaded", Name);
            return;
        }

        Logger.Information("Loading scene: {SceneName}", Name);
        OnLoad();
        IsLoaded = true;
        Logger.Information("Scene loaded: {SceneName}", Name);
    }

    /// <summary>
    /// Unloads the scene and releases its resources
    /// </summary>
    public void Unload()
    {
        if (!IsLoaded)
        {
            Logger.Warning("Scene {SceneName} is not loaded", Name);
            return;
        }

        Logger.Information("Unloading scene: {SceneName}", Name);
        OnUnload();
        IsLoaded = false;
        Logger.Information("Scene unloaded: {SceneName}", Name);
    }

    /// <summary>
    /// Updates the scene logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public void Update(GameTime gameTime)
    {
        if (!IsLoaded)
        {
            return;
        }

        OnUpdate(gameTime);
    }

    /// <summary>
    /// Draws the scene
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!IsLoaded)
        {
            return;
        }

        OnDraw(gameTime, spriteBatch);
    }

    /// <summary>
    /// Handles keyboard input when the scene has focus
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsLoaded || !HasFocus)
        {
            return;
        }

        OnHandleKeyboard(keyboardState, gameTime);
    }

    /// <summary>
    /// Handles mouse input when the scene has focus
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsLoaded || !HasFocus)
        {
            return;
        }

        OnHandleMouse(mouseState, gameTime);
    }

    /// <summary>
    /// Called when the scene is loaded. Override to implement scene-specific loading logic.
    /// </summary>
    protected abstract void OnLoad();

    /// <summary>
    /// Called when the scene is unloaded. Override to implement scene-specific unloading logic.
    /// </summary>
    protected abstract void OnUnload();

    /// <summary>
    /// Called every frame to update scene logic. Override to implement scene-specific update logic.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    protected abstract void OnUpdate(GameTime gameTime);

    /// <summary>
    /// Called every frame to draw the scene. Override to implement scene-specific drawing logic.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    protected abstract void OnDraw(GameTime gameTime, SpriteBatch spriteBatch);

    /// <summary>
    /// Called when keyboard input is received. Override to implement scene-specific keyboard handling logic.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    protected virtual void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    /// <summary>
    /// Called when mouse input is received. Override to implement scene-specific mouse handling logic.
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    protected virtual void OnHandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }
}
