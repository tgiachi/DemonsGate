using System;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     Event args for scroll value changes
/// </summary>
public class ScrollValueChangedEventArgs : EventArgs
{
    public ScrollValueChangedEventArgs(float value)
    {
        Value = value;
    }

    public float Value { get; }
}