using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Transitions.Base;

namespace SquidCraft.Client.Transitions;

/// <summary>
/// A slide transition that slides the old scene out while sliding the new scene in
/// </summary>
public class SlideTransition : BaseTransition
{
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>
    /// Initializes a new instance of the SlideTransition class
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="direction">The direction of the slide (e.g., Vector2.UnitX for right to left)</param>
    /// <param name="duration">The total duration of the slide transition</param>
    public SlideTransition(GraphicsDevice graphicsDevice, Vector2 direction, float duration = 1.0f)
        : base(duration)
    {
        _graphicsDevice = graphicsDevice;
        Direction = direction;
    }

    /// <summary>
    /// Gets the direction of the slide
    /// </summary>
    public Vector2 Direction { get; }

    /// <summary>
    /// Draws the slide transition effect
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        if (Progress < 0.5f)
        {
            // Slide out the from scene
            var fromOffset = Direction * (-viewport * (Progress * 2f));
            spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(fromOffset.X, fromOffset.Y, 0));
            FromScene?.Draw(gameTime, spriteBatch);
            spriteBatch.End();

            // Slide in the to scene
            var toOffset = Direction * (viewport * (1 - Progress * 2f));
            spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(toOffset.X, toOffset.Y, 0));
            ToScene?.Draw(gameTime, spriteBatch);
            spriteBatch.End();
        }
        else
        {
            // Continue sliding the to scene to center
            var toOffset = Direction * (viewport * (1 - (Progress - 0.5f) * 2f));
            spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(toOffset.X, toOffset.Y, 0));
            ToScene?.Draw(gameTime, spriteBatch);
            spriteBatch.End();
        }
    }
}