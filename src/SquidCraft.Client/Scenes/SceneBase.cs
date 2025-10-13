using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
}
