using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SquidCraft.Client.Transitions.Base;

namespace SquidCraft.Client.Transitions;

/// <summary>
/// An expand transition that creates an expanding rectangle effect during scene transitions
/// </summary>
public class ExpandTransition : BaseTransition
{
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>
    /// Initializes a new instance of the ExpandTransition class
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="color">The color for the expand effect</param>
    /// <param name="duration">The total duration of the expand transition</param>
    public ExpandTransition(GraphicsDevice graphicsDevice, Color color, float duration = 1.0f)
        : base(duration)
    {
        _graphicsDevice = graphicsDevice;
        Color = color;
    }

    /// <summary>
    /// Gets the color used for the expand effect
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Draws the expand transition effect
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Calculate expanding rectangle dimensions based on transition progress
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        var halfWidth = viewport.X / 2f;
        var halfHeight = viewport.Y / 2f;

        // Calculate position and size of the expanding rectangle
        var x = halfWidth * (1.0f - Progress);
        var y = halfHeight * (1.0f - Progress);
        var width = viewport.X * Progress;
        var height = viewport.Y * Progress;

        // Draw the expanding rectangle from center outward
        spriteBatch.FillRectangle(x, y, width, height, Color);
    }
}