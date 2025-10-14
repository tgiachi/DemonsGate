using FontStashSharp;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Graphics;
using SquidCraft.Client.Data;
using SquidCraft.Client.Types;

namespace SquidCraft.Client.Interfaces;

/// <summary>
/// Service interface for managing game assets including fonts, textures, sounds, and music.
/// </summary>
public interface IAssetManagerService
{
    // Font loading
    void LoadFontTtf(string path, string fontName);
    void LoadFontTtfFromStream(Stream stream, string fontName);
    SpriteFontBase GetFontTtf(string fontName, int size = 12);

    // Texture loading
    void LoadTexture(string path, string textureName);
    void LoadTextureFromStream(Stream stream, string textureName);
    Texture2D GetTexture(string textureName);

    // Sound loading
    void LoadSound(string path, string soundName);
    void LoadSoundFromStream(Stream stream, string soundName);
    SoundEffect GetSound(string soundName);

    // Music loading
    void LoadMusic(string path, string musicName);
    void LoadMusicFromStream(Stream stream, string musicName);
    Song GetMusic(string musicName);

    // Generic asset loading
    void LoadAsset(string path, string assetName);
    void LoadAssetFromStream(Stream stream, string assetName, AssetType assetType);

    // ZIP archive loading
    void LoadAssetFromZip(string zipPath, string assetPath, string assetName);
    void LoadAllAssetsFromZip(string zipPath, string baseName = "");

    // Cursor management
    void LoadCursor(string cursorSetName, CursorState cursorState, string textureName);
    void LoadCursorFromStream(Stream stream, string cursorSetName, CursorState cursorState);
    Texture2D GetCursor(string cursorSetName, CursorState cursorState);

    // Texture Atlas loading
    void LoadAtlas(string textureName, AtlasDefinition atlasDefinition, string atlasName);
    Texture2DAtlas GetAtlas(string atlasName);
    Texture2DRegion GetAtlasRegion(string atlasName, string regionName);

    // Utility
    AssetType GetTypeFromFileExtension(string extension);
}
