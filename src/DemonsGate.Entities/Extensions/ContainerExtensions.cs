using DemonsGate.Entities.Eda;
using DemonsGate.Entities.Interfaces;
using DryIoc;

namespace DemonsGate.Entities.Extensions;

public static class ContainerExtensions
{
    public static IContainer RegisterEntityServices(this IContainer container)
    {
        container.Register(typeof(IEntityDataAccess<>), typeof(AbstractEntityDataAccess<>), Reuse.Singleton);
        return container;
    }
}
