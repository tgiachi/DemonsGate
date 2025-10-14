using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.ImGUI;
using SquidCraft.Client.Interfaces.Services;

namespace SquidCraft.Client.Services;

public class ImGUIDebuggerService : IImGUIDebuggerService
{

    public bool IsEnabled { get; set; } = true;

    private readonly ImGuiRenderer _guiRenderer;
    private readonly List<ISCImGuiDebuggerComponent> _components = new();

    public ImGUIDebuggerService(Game1 game1)
    {
        _guiRenderer = new ImGuiRenderer(game1);
        _guiRenderer.RebuildFontAtlas();

    }


    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!IsEnabled)
        {
            return;
        }

        _guiRenderer.BeginLayout(gameTime);
        foreach (var component in _components)
        {
            ImGui.Begin(component.WindowTitle);
            component.Draw();
            ImGui.End();
        }
        _guiRenderer.EndLayout();

    }

    public void AddDebugger<TDebugger>(TDebugger debugger) where TDebugger : ISCImGuiDebuggerComponent
    {
        _components.Add(debugger);
    }
}
