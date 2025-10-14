using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components;
using SquidCraft.Client.Interfaces;
using SquidCraft.Client.Interfaces.Services;
using SquidCraft.Client.Services;

namespace SquidCraft.Client.Context;

public static class SquidCraftClientContext
{
    public static IAssetManagerService AssetManagerService { get; set; }
    public static ISceneManager SceneManager { get; set; }
    public static GraphicsDevice GraphicsDevice { get; set; }

    public static RootComponent RootComponent { get; set; } =  new();
}
