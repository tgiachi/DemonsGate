using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Interface for game scenes that can be managed by the SceneManager
/// </summary>
public interface IScene : ISCUpdate, ISCDrawable, ISCInputReceiver
{
    /// <summary>
    /// Unique name of the scene
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Loads the scene and initializes its resources
    /// </summary>
    void Load();

    /// <summary>
    /// Unloads the scene and releases its resources
    /// </summary>
    void Unload();
}
