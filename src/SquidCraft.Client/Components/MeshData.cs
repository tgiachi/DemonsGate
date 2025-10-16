using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Components;

/// <summary>
/// Represents mesh data for a chunk, containing vertices, indices, and texture.
/// </summary>
public sealed class MeshData
{
    /// <summary>
    /// Gets or sets the vertices of the mesh.
    /// </summary>
    public VertexPositionColorTexture[] Vertices { get; set; } = System.Array.Empty<VertexPositionColorTexture>();

    /// <summary>
    /// Gets or sets the indices of the mesh.
    /// </summary>
    public int[] Indices { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Gets or sets the texture of the mesh.
    /// </summary>
    public Texture2D? Texture { get; set; }
}