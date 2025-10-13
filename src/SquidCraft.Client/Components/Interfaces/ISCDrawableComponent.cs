using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Components.Interfaces;

public interface ISCDrawableComponent
{
    string Id { get; }

    string Name { get; }

    Vector2 Position { get; set; }

    Vector2 Scale { get; set; }

    ISCDrawableComponent? Parent { get; }

    IEnumerable<ISCDrawableComponent> Children { get; }

    int ZIndex { get; }

    bool IsVisible { get; }

    bool IsEnabled { get; }

    float Opacity { get; set; }

    float Rotation { get; set; }

    void Update(GameTime gameTime);

    void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    bool IsFocused { get; }
}
