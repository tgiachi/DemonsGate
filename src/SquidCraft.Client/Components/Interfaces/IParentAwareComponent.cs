namespace SquidCraft.Client.Components.Interfaces;

/// <summary>
/// Provides a mechanism for drawable components to keep track of their parent relationship.
/// </summary>
public interface IParentAwareComponent
{
    /// <summary>
    /// Updates the parent reference for the component.
    /// </summary>
    /// <param name="parent">The new parent component, or null when detached.</param>
    void SetParent(ISCDrawableComponent? parent);
}
