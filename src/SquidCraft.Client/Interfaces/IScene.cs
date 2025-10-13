using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for game scenes that can be managed by the SceneManager
/// </summary>
public interface IScene
{
    /// <summary>
    /// Unique name of the scene
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Loads the scene and initializes its resources
    /// </summary>
    void Load();

    /// <summary>
    /// Unloads the scene and releases its resources
    /// </summary>
    void Unload();

    /// <summary>
    /// Updates the scene logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Draws the scene
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
