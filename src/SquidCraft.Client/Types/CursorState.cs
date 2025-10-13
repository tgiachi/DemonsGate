namespace SquidCraft.Client.Types;

/// <summary>
/// Defines the possible states for cursor appearance.
/// </summary>
public enum CursorState
{
    /// <summary>
    /// Default cursor state
    /// </summary>
    Default,

    /// <summary>
    /// Cursor hovering over an interactive element
    /// </summary>
    Hover,

    /// <summary>
    /// Cursor in pressed/clicking state
    /// </summary>
    Pressed,

    /// <summary>
    /// Cursor in disabled state
    /// </summary>
    Disabled,

    /// <summary>
    /// Cursor indicating text selection
    /// </summary>
    Text,

    /// <summary>
    /// Cursor indicating a hand/pointer
    /// </summary>
    Pointer,

    /// <summary>
    /// Cursor indicating a resize operation
    /// </summary>
    Resize,

    /// <summary>
    /// Cursor indicating a move operation
    /// </summary>
    Move,

    /// <summary>
    /// Cursor indicating a wait/loading state
    /// </summary>
    Wait
}
