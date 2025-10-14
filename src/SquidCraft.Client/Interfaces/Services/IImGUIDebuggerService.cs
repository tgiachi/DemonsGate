using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Interfaces.Services;

public interface IImGUIDebuggerService : ISCDrawable
{
    void AddDebugger<TDebugger>(TDebugger debugger) where TDebugger : ISCImGuiDebuggerComponent;

}
