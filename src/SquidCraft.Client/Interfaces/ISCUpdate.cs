using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for components that can be updated each frame
/// </summary>
public interface ISCUpdate
{
    /// <summary>
    /// Updates the component logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    void Update(GameTime gameTime);
}
