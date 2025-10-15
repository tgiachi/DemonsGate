using SquidCraft.Client.Collections;
using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Interfaces;

public interface IScene : ISCUpdate, ISCDrawable, ISCInputReceiver
{
    string Name { get; }

    SCDrawableCollection<ISCDrawableComponent> Components { get; }

    void Load();

    void Unload();
}
