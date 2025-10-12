using SquidCraft.Entities.Eda;
using SquidCraft.Entities.Interfaces;
using DryIoc;

namespace SquidCraft.Entities.Extensions;

public static class ContainerExtensions
{
    public static IContainer RegisterEntityServices(this IContainer container)
    {
        container.Register(typeof(IEntityDataAccess<>), typeof(AbstractEntityDataAccess<>), Reuse.Singleton);
        return container;
    }
}
