using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for components that can be drawn each frame
/// </summary>
public interface ISCDrawable
{
    /// <summary>
    /// Draws the component
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
