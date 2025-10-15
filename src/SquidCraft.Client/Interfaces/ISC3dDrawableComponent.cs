using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Interfaces;

public interface ISC3dDrawableComponent : ISCUpdate, ISCInputReceiver
{
    string Id { get; }

    string Name { get; set; }

    Vector3 Position { get; set; }

    Vector3 Rotation { get; set; }

    Vector3 Scale { get; set; }

    bool IsVisible { get; set; }

    bool IsEnabled { get; set; }

    float Opacity { get; set; }

    void Draw3d(GameTime gameTime);
}
