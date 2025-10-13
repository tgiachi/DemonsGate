using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SquidCraft.Client.Transitions.Base;

namespace SquidCraft.Client.Transitions;

/// <summary>
/// A glitch transition that creates digital distortion effects during scene transitions
/// </summary>
public class GlitchTransition : BaseTransition
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the GlitchTransition class
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="glitchIntensity">Intensity of the glitch effect (0.0 to 1.0)</param>
    /// <param name="glitchBlocks">Number of glitch blocks to render</param>
    /// <param name="duration">The total duration of the glitch transition</param>
    public GlitchTransition(GraphicsDevice graphicsDevice, float glitchIntensity = 0.8f, int glitchBlocks = 20, float duration = 1.0f)
        : base(duration)
    {
        _graphicsDevice = graphicsDevice;
        GlitchIntensity = MathHelper.Clamp(glitchIntensity, 0f, 1f);
        GlitchBlocks = Math.Max(1, glitchBlocks);
        _random = new Random();
    }

    /// <summary>
    /// Gets the intensity of the glitch effect
    /// </summary>
    public float GlitchIntensity { get; }

    /// <summary>
    /// Gets the number of glitch blocks
    /// </summary>
    public int GlitchBlocks { get; }

    /// <summary>
    /// Draws the glitch transition effect
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        if (Progress < 0.5f)
        {
            // Phase 1: Glitch out - Draw from scene with increasing glitch effect
            if (FromScene != null)
            {
                FromScene.Draw(gameTime, spriteBatch);
            }

            // Apply glitch effect that gets more intense over time
            DrawGlitchEffect(spriteBatch, Progress * 2f);
        }
        else
        {
            // Phase 2: Glitch in - Draw to scene with decreasing glitch effect
            if (ToScene != null)
            {
                ToScene.Draw(gameTime, spriteBatch);
            }

            // Apply glitch effect that gets less intense over time
            DrawGlitchEffect(spriteBatch, (1.0f - Progress) * 2f);
        }
    }

    /// <summary>
    /// Draws the glitch distortion effect
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to use for drawing</param>
    /// <param name="intensity">Current intensity of the glitch effect (0.0 to 1.0)</param>
    private void DrawGlitchEffect(SpriteBatch spriteBatch, float intensity)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        var currentIntensity = MathHelper.Clamp(intensity * GlitchIntensity, 0f, 1f);

        if (currentIntensity <= 0.001f || viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var blockCount = Math.Max(1, (int)(GlitchBlocks * currentIntensity));

        // Draw random colored rectangles to simulate digital glitches
        for (var i = 0; i < blockCount; i++)
        {
            // Generate safe random values
            var maxX = Math.Max(1, (int)viewport.X - 1);
            var maxY = Math.Max(1, (int)viewport.Y - 1);
            var x = _random.Next(0, maxX);
            var y = _random.Next(0, maxY);

            var maxWidth = Math.Max(10, (int)(viewport.X * 0.3f * currentIntensity));
            var maxHeight = Math.Max(2, (int)(20 * currentIntensity));

            var width = _random.Next(10, Math.Max(11, maxWidth));
            var height = _random.Next(2, Math.Max(3, maxHeight));

            // Ensure the block stays within screen bounds
            width = Math.Min(width, (int)viewport.X - x);
            height = Math.Min(height, (int)viewport.Y - y);

            // Skip if dimensions are invalid
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            // Generate glitch colors
            var glitchColor = GetRandomGlitchColor(currentIntensity);

            // Draw the glitch block
            spriteBatch.FillRectangle(x, y, width, height, glitchColor);
        }

        // Add horizontal line glitches
        DrawHorizontalGlitches(spriteBatch, currentIntensity);

        // Add vertical displacement glitches
        DrawVerticalDisplacement(spriteBatch, currentIntensity);
    }

    /// <summary>
    /// Draws horizontal line glitches
    /// </summary>
    private void DrawHorizontalGlitches(SpriteBatch spriteBatch, float intensity)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var lineCount = Math.Max(0, (int)(GlitchBlocks * 0.5f * intensity));

        for (var i = 0; i < lineCount; i++)
        {
            var maxY = Math.Max(1, (int)viewport.Y - 1);
            var y = _random.Next(0, maxY);
            var width = viewport.X;
            var maxHeight = Math.Max(1, (int)(4 * intensity));
            var height = _random.Next(1, Math.Max(2, maxHeight));

            // Ensure within bounds
            height = Math.Min(height, (int)viewport.Y - y);
            if (height <= 0)
            {
                continue;
            }

            var glitchColor = GetRandomGlitchColor(intensity * 0.6f);

            spriteBatch.FillRectangle(0, y, width, height, glitchColor);
        }
    }

    /// <summary>
    /// Draws vertical displacement glitches
    /// </summary>
    private void DrawVerticalDisplacement(SpriteBatch spriteBatch, float intensity)
    {
        var viewport = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var blockCount = Math.Max(0, (int)(GlitchBlocks * 0.3f * intensity));

        for (var i = 0; i < blockCount; i++)
        {
            var maxX = Math.Max(1, (int)viewport.X - 1);
            var maxY = Math.Max(1, (int)viewport.Y - 1);
            var x = _random.Next(0, maxX);
            var y = _random.Next(0, maxY);

            var maxWidth = Math.Max(2, (int)(8 * intensity));
            var maxHeight = Math.Max(10, (int)(viewport.Y * 0.2f * intensity));

            var width = _random.Next(2, Math.Max(3, maxWidth));
            var height = _random.Next(10, Math.Max(11, maxHeight));

            // Ensure the block stays within screen bounds
            width = Math.Min(width, (int)viewport.X - x);
            height = Math.Min(height, (int)viewport.Y - y);

            // Skip if dimensions are invalid
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            var glitchColor = GetRandomGlitchColor(intensity * 0.4f);

            spriteBatch.FillRectangle(x, y, width, height, glitchColor);
        }
    }

    /// <summary>
    /// Gets a random glitch color with the specified intensity
    /// </summary>
    private Color GetRandomGlitchColor(float intensity)
    {
        var alpha = (byte)(255 * intensity);

        // Classic glitch colors: cyan, magenta, red, green, blue
        var glitchColors = new[]
        {
            new Color((byte)0, (byte)255, (byte)255, alpha),  // Cyan
            new Color((byte)255, (byte)0, (byte)255, alpha),  // Magenta
            new Color((byte)255, (byte)0, (byte)0, alpha),    // Red
            new Color((byte)0, (byte)255, (byte)0, alpha),    // Green
            new Color((byte)0, (byte)0, (byte)255, alpha),    // Blue
            new Color((byte)255, (byte)255, (byte)0, alpha),  // Yellow
            new Color((byte)255, (byte)255, (byte)255, alpha) // White
        };

        return glitchColors[_random.Next(glitchColors.Length)];
    }
}