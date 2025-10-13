using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Services;

namespace SquidCraft.Client.Context;

public static class SquidCraftClientContext
{
    public static AssetManagerService AssetManagerService { get; set; }

    public static GraphicsDevice GraphicsDevice { get; set; }
}
