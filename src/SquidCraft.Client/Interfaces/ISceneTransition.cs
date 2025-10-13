using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for scene transitions that handle the visual transition between scenes
/// </summary>
public interface ISceneTransition : IDisposable
{
    /// <summary>
    /// The scene being transitioned from
    /// </summary>
    IScene? FromScene { get; }

    /// <summary>
    /// The scene being transitioned to
    /// </summary>
    IScene? ToScene { get; }

    /// <summary>
    /// Whether the transition has completed
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Event fired when the transition completes
    /// </summary>
    event EventHandler? Completed;

    /// <summary>
    /// Starts the transition between two scenes
    /// </summary>
    /// <param name="fromScene">The scene to transition from</param>
    /// <param name="toScene">The scene to transition to</param>
    void Start(IScene? fromScene, IScene toScene);

    /// <summary>
    /// Updates the transition
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Draws the transition
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
