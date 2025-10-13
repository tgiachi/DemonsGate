using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SquidCraft.Client.Transitions.Base;

namespace SquidCraft.Client.Transitions;

/// <summary>
/// A fade transition that fades to a specified color during scene transitions
/// </summary>
public class FadeTransition : BaseTransition
{
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="color">The color to fade to</param>
    /// <param name="duration">The total duration of the fade transition</param>
    public FadeTransition(GraphicsDevice graphicsDevice, Color color, float duration = 1.0f)
        : base(duration)
    {
        _graphicsDevice = graphicsDevice;
        Color = color;
    }

    /// <summary>
    /// Initializes a new instance of the FadeTransition class with default black color
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="duration">The total duration of the fade transition</param>
    public FadeTransition(GraphicsDevice graphicsDevice, float duration = 1.0f)
        : this(graphicsDevice, Color.Black, duration)
    {
    }

    /// <summary>
    /// Gets the color used for the fade effect
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Draws the fade transition effect
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        if (Progress < 0.5f)
        {
            // Phase 1: Fade out - Draw from scene with increasing fade overlay
            if (FromScene != null)
            {
                FromScene.Draw(gameTime, spriteBatch);
            }

            // Draw fade overlay that gets more opaque over time
            var fadeAlpha = Progress * 2f; // 0.0 to 1.0 during fade out
            var fadeColor = Color * fadeAlpha;

            // Use the provided SpriteBatch to draw the fade overlay
            spriteBatch.FillRectangle(
                Vector2.Zero,
                viewport,
                fadeColor
            );
        }
        else
        {
            // Phase 2: Fade in - Draw to scene with decreasing fade overlay
            if (ToScene != null)
            {
                ToScene.Draw(gameTime, spriteBatch);
            }

            // Draw fade overlay that gets more transparent over time
            var fadeAlpha = 1.0f - ((Progress - 0.5f) * 2f); // 1.0 to 0.0 during fade in
            var fadeColor = Color * fadeAlpha;

            // Use the provided SpriteBatch to draw the fade overlay
            spriteBatch.FillRectangle(
                Vector2.Zero,
                viewport,
                fadeColor
            );
        }
    }
}