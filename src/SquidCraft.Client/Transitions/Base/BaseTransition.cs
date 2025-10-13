using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Transitions.Base;

/// <summary>
/// Base implementation of ISceneTransition providing common transition functionality
/// </summary>
public abstract class BaseTransition : ISceneTransition
{
    private readonly float _duration;
    private float _elapsed;

    /// <summary>
    /// Initializes a new instance of the BaseTransition class
    /// </summary>
    /// <param name="duration">The total duration of the transition</param>
    protected BaseTransition(float duration)
    {
        _duration = duration;
    }

    /// <summary>
    /// The scene being transitioned from
    /// </summary>
    public IScene? FromScene { get; private set; }

    /// <summary>
    /// The scene being transitioned to
    /// </summary>
    public IScene? ToScene { get; private set; }

    /// <summary>
    /// Whether the transition has completed
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Event fired when the transition completes
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Gets the total duration of the transition
    /// </summary>
    protected float Duration => _duration;

    /// <summary>
    /// Gets the current progress of the transition (0.0 to 1.0)
    /// </summary>
    protected float Progress => MathHelper.Clamp(_elapsed / _duration, 0f, 1f);

    /// <summary>
    /// Starts the transition between two scenes
    /// </summary>
    /// <param name="fromScene">The scene to transition from</param>
    /// <param name="toScene">The scene to transition to</param>
    public void Start(IScene? fromScene, IScene toScene)
    {
        FromScene = fromScene;
        ToScene = toScene;
        _elapsed = 0f;
        IsCompleted = false;
    }

    /// <summary>
    /// Updates the transition
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public void Update(GameTime gameTime)
    {
        if (IsCompleted)
        {
            return;
        }

        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_elapsed >= _duration)
        {
            IsCompleted = true;
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Draws the transition
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    /// <summary>
    /// Disposes the transition resources
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}