using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Components.UI.Controls;

public class WatchTextComponent : TextComponent
{
    private readonly TimeSpan _updateInterval;

    private TimeSpan _currentInterval;

    private readonly Func<string> _onTextChanged;


    public WatchTextComponent(Vector2 position, TimeSpan updateEvery, Func<string> onTextChanged) : base(fontSize: 14)
    {
        Position = position;
        _onTextChanged = onTextChanged ?? throw new ArgumentNullException(nameof(onTextChanged));

        _updateInterval = updateEvery;
        _currentInterval = TimeSpan.Zero;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _currentInterval += gameTime.ElapsedGameTime;

        if (_currentInterval >= _updateInterval)
        {
            _currentInterval = TimeSpan.Zero;
            Text = _onTextChanged();
        }
    }
}
