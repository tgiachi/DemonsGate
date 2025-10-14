using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Components;

/// <summary>
/// Text-based HUD component that displays the current frames per second.
/// </summary>
public sealed class FpsComponent : TextComponent
{
    private float _accumulatedTime;
    private int _frameCounter;
    private float _smoothingInterval = 0.5f;

    /// <summary>
    /// Initializes a new instance of the <see cref="FpsComponent"/>.
    /// </summary>
    /// <param name="fontName">Font used to render the counter.</param>
    /// <param name="fontSize">Font size used for rendering.</param>
    /// <param name="position">Screen position anchored to the component.</param>
    /// <param name="color">Text color of the counter.</param>
    public FpsComponent(
        string fontName = "DefaultFont",
        int fontSize = 14,
        Vector2? position = null,
        Color? color = null)
        : base("FPS: --", fontName, fontSize, position, color)
    {
    }

    /// <summary>
    /// Most recently calculated frame rate.
    /// </summary>
    public int LastFrameRate { get; private set; }

    /// <summary>
    /// Rolling window used when computing the frame rate.
    /// </summary>
    public float SmoothingIntervalSeconds
    {
        get => _smoothingInterval;
        set => _smoothingInterval = MathHelper.Max(0.1f, value);
    }

    /// <summary>
    /// Updates the FPS counter once per smoothing interval.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        _accumulatedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _frameCounter++;

        if (_accumulatedTime >= _smoothingInterval)
        {
            var fps = _frameCounter / _accumulatedTime;
            LastFrameRate = (int)MathF.Round(fps);
            Text = $"FPS: {LastFrameRate}";

            _accumulatedTime = 0f;
            _frameCounter = 0;
        }

        base.Update(gameTime);
    }
}
