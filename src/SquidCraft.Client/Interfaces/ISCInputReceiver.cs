using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for components that can receive keyboard and mouse input events
/// </summary>
public interface ISCInputReceiver
{
    /// <summary>
    /// Gets or sets whether this component has input focus for keyboard and mouse events
    /// </summary>
    bool HasFocus { get; set; }

    /// <summary>
    /// Handles keyboard input when the component has focus
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime);

    /// <summary>
    /// Handles mouse input when the component has focus
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    void HandleMouse(MouseState mouseState, GameTime gameTime);
}
