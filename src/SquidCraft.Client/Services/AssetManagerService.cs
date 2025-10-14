using System.IO.Compression;
using System.Text.Json;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidCraft.Client.Data;
using SquidCraft.Client.Interfaces;
using SquidCraft.Client.Types;

namespace SquidCraft.Client.Services;

/// <summary>
/// Implementation of the asset manager service responsible for loading, caching, and retrieving game assets.
/// Manages fonts, textures, sounds, and music with automatic lifecycle integration.
/// </summary>
public class AssetManagerService : IAssetManagerService
{
    /// <summary>
    /// Default font sizes automatically generated when loading TrueType fonts
    /// </summary>
    private static readonly int[] DefaultFontSizes = [8, 10, 12, 14, 16, 18, 20, 22, 24, 32, 48, 64];

    /// <summary>
    /// Registry of loaded assets organized by asset type
    /// </summary>
    private readonly Dictionary<AssetType, List<AssetObject>> _assets = new();


    /// <summary>
    /// Font systems for TrueType fonts, enabling dynamic size generation
    /// </summary>
    private readonly Dictionary<string, FontSystem> _fontsSystems = new();

    /// <summary>
    /// Graphics device reference for loading GPU resources
    /// </summary>
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>
    /// Cache of loaded cursor sets, organized by cursor set name and cursor state
    /// </summary>
    private readonly Dictionary<(string CursorSetName, CursorState State), Texture2D> _loadedCursors = new();

    /// <summary>
    /// Cache of loaded TrueType fonts indexed by name and size
    /// </summary>
    private readonly Dictionary<(string FontName, int Size), SpriteFontBase> _loadedFontsTtf = new();

    /// <summary>
    /// Cache of loaded music tracks indexed by name
    /// </summary>
    private readonly Dictionary<string, Song> _loadedMusic = new();

    /// <summary>
    /// Cache of loaded sound effects indexed by name
    /// </summary>
    private readonly Dictionary<string, SoundEffect> _loadedSounds = new();

    /// <summary>
    /// Cache of loaded textures indexed by name
    /// </summary>
    private readonly Dictionary<string, Texture2D> _loadedTextures = new();

    /// <summary>
    /// Cache of loaded texture atlases indexed by name
    /// </summary>
    private readonly Dictionary<string, (Texture2DAtlas Atlas, int Rows, int Cols)> _loadedAtlases = new();

    /// <summary>
    /// Lazy-initialized single pixel texture for primitive rendering helpers.
    /// </summary>
    private Texture2D? _pixelTexture;

    /// <summary>
    /// Structured logger instance for asset management operations
    /// </summary>
    private readonly ILogger _logger = Log.ForContext<AssetManagerService>();


    private readonly string _rootDirectory;

    /// <summary>
    /// Initializes a new instance of the AssetManagerService with the specified dependencies
    /// </summary>
    /// <param name="directoriesConfig">Directory configuration for asset path resolution</param>
    /// <param name="graphicsDevice">Graphics device for loading GPU resources</param>
    public AssetManagerService(string rootDirectory, GraphicsDevice graphicsDevice)
    {
        _logger.Information("Initializing AssetManagerService");
        _graphicsDevice = graphicsDevice;
        _rootDirectory = Path.Combine(rootDirectory, "Assets");
    }

    /// <summary>
    /// Loads a TrueType font from the specified file path and generates multiple sizes for immediate use.
    /// The font system enables dynamic size generation for any required font size.
    /// </summary>
    /// <param name="path">Relative path to the font file from the content root directory</param>
    /// <param name="fontName">Unique name to identify and retrieve the loaded font</param>
    /// <exception cref="FileNotFoundException">Thrown when the font file is not found at the specified path</exception>
    public void LoadFontTtf(string path, string fontName)
    {
        _logger.Debug("Loading font: {Path} - {FontName}", path, fontName);
        var fontPath = Path.Combine(_rootDirectory, path);

        if (!File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Font not found: {fontPath}");
        }

        _assets.TryAdd(AssetType.FontTtf, new List<AssetObject>());
        _assets[AssetType.FontTtf].Add(new AssetObject(fontName, fontPath));

        var fontSystem = new FontSystem();
        fontSystem.AddFont(File.ReadAllBytes(fontPath));
        _fontsSystems[fontName] = fontSystem;

        foreach (var size in DefaultFontSizes)
        {
            var spriteFont = fontSystem.GetFont(size);
            var key = (fontName, size);
            _loadedFontsTtf[key] = spriteFont;
            _logger.Debug("Loaded font size: {Size} - Key: {Key}", size, key);
        }

        _logger.Information("Font loaded: {Path} - {FontName}", path, fontName);
    }

    /// <summary>
    /// Loads an asset from the specified path, automatically determining the asset type from the file extension.
    /// Routes to the appropriate specialized loader based on the detected asset type.
    /// </summary>
    /// <param name="path">Relative path to the asset file from the content root directory</param>
    /// <param name="assetName">Unique name to identify and retrieve the loaded asset</param>
    public void LoadAsset(string path, string assetName)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var assetType = GetTypeFromFileExtension(extension);

        switch (assetType)
        {
            case AssetType.FontTtf:
                LoadFontTtf(path, assetName);
                break;
            case AssetType.Image:
                LoadTexture(path, assetName);
                break;
            case AssetType.Sound:
                LoadSound(path, assetName);
                break;
            case AssetType.Music:
                LoadMusic(path, assetName);
                break;
            default:
                _logger.Warning("Unsupported asset type for path: {Path}", path);
                break;
        }
    }

    /// <summary>
    /// Loads an asset from a stream, specifying the asset type explicitly.
    /// Routes to the appropriate specialized loader based on the provided asset type.
    /// Useful for loading assets from embedded resources, network streams, or other sources.
    /// </summary>
    /// <param name="stream">Stream containing the asset data</param>
    /// <param name="assetName">Unique name to identify and retrieve the loaded asset</param>
    /// <param name="assetType">Type of the asset to load from the stream</param>
    public void LoadAssetFromStream(Stream stream, string assetName, AssetType assetType)
    {
        switch (assetType)
        {
            case AssetType.FontTtf:
                LoadFontTtfFromStream(stream, assetName);
                break;
            case AssetType.Image:
                LoadTextureFromStream(stream, assetName);
                break;
            case AssetType.Sound:
                LoadSoundFromStream(stream, assetName);
                break;
            case AssetType.Music:
                LoadMusicFromStream(stream, assetName);
                break;
            default:
                _logger.Warning("Unsupported asset type for stream loading: {AssetType}", assetType);
                break;
        }
    }

    /// <summary>
    /// Loads a texture from the specified file path for use in sprite rendering.
    /// Supports common image formats including PNG, JPG, and others supported by MonoGame.
    /// </summary>
    /// <param name="path">Relative path to the texture file from the content root directory</param>
    /// <param name="textureName">Unique name to identify and retrieve the loaded texture</param>
    /// <exception cref="FileNotFoundException">Thrown when the texture file is not found at the specified path</exception>
    public void LoadTexture(string path, string textureName)
    {
        _logger.Debug("Loading texture: {Path} - {TextureName}", path, textureName);
        var texturePath = Path.Combine(_rootDirectory, path);

        if (!File.Exists(texturePath))
        {
            throw new FileNotFoundException($"Texture not found: {texturePath}");
        }

        _assets.TryAdd(AssetType.Image, new List<AssetObject>());
        _assets[AssetType.Image].Add(new AssetObject(textureName, texturePath));

        using var stream = File.OpenRead(texturePath);
        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        _loadedTextures[textureName] = texture;

        _logger.Information("Texture loaded: {Path} - {TextureName}", path, textureName);
    }

    /// <summary>
    /// Loads a texture from an input stream for use in sprite rendering.
    /// Useful for loading textures from embedded resources or network streams.
    /// </summary>
    /// <param name="stream">Stream containing the texture data</param>
    /// <param name="textureName">Unique name to identify and retrieve the loaded texture</param>
    public void LoadTextureFromStream(Stream stream, string textureName)
    {
        _logger.Debug("Loading texture from stream - {TextureName}", textureName);

        _assets.TryAdd(AssetType.Image, new List<AssetObject>());
        _assets[AssetType.Image].Add(new AssetObject(textureName, "Stream"));

        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        _loadedTextures[textureName] = texture;

        _logger.Information("Texture loaded from stream - {TextureName}", textureName);
    }

    /// <summary>
    /// Retrieves a reusable 1x1 white texture useful for drawing rectangles and lines.
    /// </summary>
    public Texture2D GetPixelTexture()
    {
        if (_pixelTexture == null || _pixelTexture.IsDisposed || _pixelTexture.GraphicsDevice != _graphicsDevice)
        {
            _pixelTexture?.Dispose();
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData([Color.White]);
        }

        return _pixelTexture;
    }

    /// <summary>
    /// Loads a TrueType font from an input stream and generates multiple sizes for immediate use.
    /// Useful for loading fonts from embedded resources or network streams.
    /// </summary>
    /// <param name="stream">Stream containing the font data</param>
    /// <param name="fontName">Unique name to identify and retrieve the loaded font</param>
    public void LoadFontTtfFromStream(Stream stream, string fontName)
    {
        _logger.Debug("Loading font from stream - {FontName}", fontName);

        _assets.TryAdd(AssetType.FontTtf, new List<AssetObject>());
        _assets[AssetType.FontTtf].Add(new AssetObject(fontName, "Stream"));

        var fontSystem = new FontSystem();

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var fontData = memoryStream.ToArray();

        fontSystem.AddFont(fontData);
        _fontsSystems[fontName] = fontSystem;

        foreach (var size in DefaultFontSizes)
        {
            var spriteFont = fontSystem.GetFont(size);
            var key = (fontName, size);
            _loadedFontsTtf[key] = spriteFont;
            _logger.Debug("Loaded font size: {Size} - Key: {Key}", size, key);
        }

        _logger.Information("Font loaded from stream - {FontName}", fontName);
    }

    /// <summary>
    /// Retrieves a previously loaded TrueType font at the specified size.
    /// If the requested size hasn't been generated, it will be created on demand using the font system.
    /// </summary>
    /// <param name="fontName">Name of the font as registered during loading</param>
    /// <param name="size">Font size in pixels (default: 12)</param>
    /// <returns>SpriteFontBase instance ready for text rendering operations</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the font name is not found in the loaded fonts</exception>
    public SpriteFontBase GetFontTtf(string fontName, int size = 12)
    {
        var key = (fontName, size);

        if (_loadedFontsTtf.TryGetValue(key, out var font))
        {
            return font;
        }

        if (_fontsSystems.TryGetValue(fontName, out var fontSystem))
        {
            font = fontSystem.GetFont(size);
            _loadedFontsTtf[key] = font;
            return font;
        }

        throw new KeyNotFoundException($"Font not found: {fontName} with size {size}");
    }

    /// <summary>
    /// Retrieves a previously loaded texture by name for use in rendering operations.
    /// Textures are cached and ready for immediate use in sprite batch operations.
    /// </summary>
    /// <param name="textureName">Name of the texture as registered during loading</param>
    /// <returns>Texture2D instance ready for rendering operations</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the texture name is not found in the loaded textures</exception>
    public Texture2D GetTexture(string textureName)
    {
        return _loadedTextures.TryGetValue(textureName, out var texture)
            ? texture
            : throw new KeyNotFoundException($"Texture not found: {textureName}");
    }


    public void LoadAtlas(string textureName, AtlasDefinition atlasDefinition, string atlasName)
    {
        _logger.Debug("Loading atlas: {AtlasName}", atlasName);

        var texture = GetTexture(textureName);

        int cols = (texture.Width - atlasDefinition.Margin * 2) / (atlasDefinition.TileWidth + atlasDefinition.Spacing);
        int rows = (texture.Height - atlasDefinition.Margin * 2) / (atlasDefinition.TileHeight + atlasDefinition.Spacing);
        int maxTiles = rows * cols;

        var atlas = Texture2DAtlas.Create(
            atlasName,
            texture,
            atlasDefinition.TileWidth,
            atlasDefinition.TileHeight,
            maxTiles,
            atlasDefinition.Margin,
            atlasDefinition.Spacing
        );

        _loadedAtlases[atlasName] = (atlas, rows, cols);

        _assets.TryAdd(AssetType.Atlas, []);
        _assets[AssetType.Atlas].Add(new AssetObject(atlasName, textureName));

        _logger.Information(
            "Atlas loaded: {AtlasName} with {RegionCount} regions ({Rows}x{Cols})",
            atlasName,
            maxTiles,
            rows,
            cols
        );
    }

    /// <summary>
    /// Retrieves a previously loaded texture atlas by name.
    /// </summary>
    /// <param name="atlasName">Name of the atlas as registered during loading</param>
    /// <returns>Texture2DAtlas instance containing all texture regions</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the atlas name is not found</exception>
    public Texture2DAtlas GetAtlas(string atlasName)
    {
        return !_loadedAtlases.TryGetValue(atlasName, out var entry) ? throw new KeyNotFoundException($"Atlas not found: {atlasName}") : entry.Atlas;
    }

    /// <summary>
    /// Retrieves a specific texture region from a loaded atlas.
    /// </summary>
    /// <param name="atlasName">Name of the atlas containing the region</param>
    /// <param name="regionName">Name of the specific region to retrieve</param>
    /// <returns>Texture2DRegion instance for the specified region</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the atlas or region is not found</exception>
    public Texture2DRegion GetAtlasRegion(string atlasName, string regionName)
    {
        if (!_loadedAtlases.TryGetValue(atlasName, out var entry))
        {
            throw new KeyNotFoundException($"Atlas '{atlasName}' not found");
        }

        var parts = regionName.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col) && row >= 0 && row < entry.Rows && col >= 0 && col < entry.Cols)
        {
            int index = row * entry.Cols + col;
            return entry.Atlas[index];
        }

        throw new KeyNotFoundException($"Region '{regionName}' not found in atlas '{atlasName}'");
    }

    /// <summary>
    /// Determines the asset type based on the file extension.
    /// Used to categorize assets during loading and management operations.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".png", ".ttf")</param>
    /// <returns>AssetType enum value corresponding to the file extension</returns>
    public AssetType GetTypeFromFileExtension(string extension)
    {
        return extension switch
        {
            ".ttf" or ".otf"                                => AssetType.FontTtf,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => AssetType.Image,
            ".atlas"                                        => AssetType.Atlas,
            ".wav" or ".mp3" or ".ogg"                      => AssetType.Sound,
            ".wma" or ".m4a"                                => AssetType.Music,
            _                                               => AssetType.Unknown
        };
    }

    /// <summary>
    /// Loads an asset from a ZIP archive by specifying the ZIP file path and the internal path of the asset.
    /// Automatically determines the asset type from the file extension and routes to the appropriate loader.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP archive file</param>
    /// <param name="assetPath">Internal path of the asset within the ZIP archive</param>
    /// <param name="assetName">Unique name to identify and retrieve the loaded asset</param>
    /// <exception cref="FileNotFoundException">Thrown when the ZIP file is not found</exception>
    /// <exception cref="InvalidDataException">Thrown when the asset is not found in the ZIP archive</exception>
    public void LoadAssetFromZip(string zipPath, string assetPath, string assetName)
    {
        var extension = Path.GetExtension(assetPath).ToLowerInvariant();
        var assetType = GetTypeFromFileExtension(extension);

        using var zipStream = ExtractFileFromZip(zipPath, assetPath);
        LoadAssetFromStream(zipStream, assetName, assetType);
    }

    /// <summary>
    /// Loads a texture from a ZIP archive.
    /// Useful for loading textures from packaged asset archives.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP archive file</param>
    /// <param name="assetPath">Internal path of the texture within the ZIP archive</param>
    /// <param name="textureName">Unique name to identify the loaded texture</param>
    /// <exception cref="FileNotFoundException">Thrown when the ZIP file is not found</exception>
    /// <exception cref="InvalidDataException">Thrown when the texture is not found in the ZIP archive</exception>
    public void LoadTextureFromZip(string zipPath, string assetPath, string textureName)
    {
        using var zipStream = ExtractFileFromZip(zipPath, assetPath);
        LoadTextureFromStream(zipStream, textureName);
    }

    /// <summary>
    /// Loads a TrueType font from a ZIP archive.
    /// The font will be available in multiple default sizes for immediate use.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP archive file</param>
    /// <param name="assetPath">Internal path of the font within the ZIP archive</param>
    /// <param name="fontName">Unique name to identify the loaded font</param>
    /// <exception cref="FileNotFoundException">Thrown when the ZIP file is not found</exception>
    /// <exception cref="InvalidDataException">Thrown when the font is not found in the ZIP archive</exception>
    public void LoadFontTtfFromZip(string zipPath, string assetPath, string fontName)
    {
        using var zipStream = ExtractFileFromZip(zipPath, assetPath);
        LoadFontTtfFromStream(zipStream, fontName);
    }

    /// <summary>
    /// Loads all supported assets from a ZIP archive.
    /// Automatically detects asset types based on file extensions and loads them with appropriate names.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP archive file</param>
    /// <param name="baseName">Base name to use for asset naming (files will be named as baseName_fileNameWithoutExtension)</param>
    /// <exception cref="FileNotFoundException">Thrown when the ZIP file is not found</exception>
    public void LoadAllAssetsFromZip(string zipPath, string baseName = "")
    {
        _logger.Debug("Loading all assets from ZIP: {ZipPath}", zipPath);

        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP file not found: {zipPath}");
        }

        using var zipArchive = ZipFile.OpenRead(zipPath);

        foreach (var entry in zipArchive.Entries)
        {
            if (entry.Length == 0)
            {
                continue; // Skip directories
            }

            var extension = Path.GetExtension(entry.FullName).ToLowerInvariant();
            var assetType = GetTypeFromFileExtension(extension);

            if (assetType == AssetType.Unknown)
            {
                continue; // Skip unsupported files
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.Name);
            var assetName = string.IsNullOrEmpty(baseName)
                ? fileNameWithoutExtension
                : $"{baseName}_{fileNameWithoutExtension}";

            try
            {
                using var stream = entry.Open();
                LoadAssetFromStream(stream, assetName, assetType);
                _logger.Debug("Loaded asset from ZIP: {AssetName} ({AssetType})", assetName, assetType);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load asset from ZIP: {AssetPath}", entry.FullName);
            }
        }

        _logger.Information("Loaded all assets from ZIP: {ZipPath}", zipPath);
    }

    /// <summary>
    /// Loads a cursor texture for a specific cursor state within a cursor set.
    /// Cursor sets allow grouping related cursor textures together.
    /// </summary>
    /// <param name="cursorSetName">Name of the cursor set to load the cursor into</param>
    /// <param name="cursorState">The cursor state this texture represents</param>
    /// <param name="textureName">Name of the texture to use for this cursor state</param>
    /// <exception cref="KeyNotFoundException">Thrown when the texture name is not found</exception>
    public void LoadCursor(string cursorSetName, CursorState cursorState, string textureName)
    {
        if (!_loadedTextures.TryGetValue(textureName, out var texture))
        {
            throw new KeyNotFoundException(
                $"Texture '{textureName}' not found. Load the texture first before using it as a cursor."
            );
        }

        _loadedCursors[(cursorSetName, cursorState)] = texture;

        _logger.Debug(
            "Loaded cursor {CursorState} for set {CursorSetName} using texture {TextureName}",
            cursorState,
            cursorSetName,
            textureName
        );
    }

    /// <summary>
    /// Loads a cursor texture from a stream for a specific cursor state within a cursor set.
    /// Cursor sets allow grouping related cursor textures together.
    /// </summary>
    /// <param name="stream">Stream containing the cursor texture data</param>
    /// <param name="cursorSetName">Name of the cursor set to load the cursor into</param>
    /// <param name="cursorState">The cursor state this texture represents</param>
    public void LoadCursorFromStream(Stream stream, string cursorSetName, CursorState cursorState)
    {
        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        _loadedCursors[(cursorSetName, cursorState)] = texture;

        _logger.Debug(
            "Loaded cursor {CursorState} for set {CursorSetName} from stream",
            cursorState,
            cursorSetName
        );
    }

    /// <summary>
    /// Retrieves a cursor texture for a specific cursor state from a cursor set.
    /// </summary>
    /// <param name="cursorSetName">Name of the cursor set containing the cursor</param>
    /// <param name="cursorState">The cursor state to retrieve</param>
    /// <returns>Texture2D instance for the specified cursor state</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the cursor set or cursor state is not found</exception>
    public Texture2D GetCursor(string cursorSetName, CursorState cursorState)
    {
        if (!_loadedCursors.TryGetValue((cursorSetName, cursorState), out var cursor))
        {
            throw new KeyNotFoundException(
                $"Cursor state '{cursorState}' not found in set '{cursorSetName}'. Load this cursor state first."
            );
        }

        return cursor;
    }

    /// <summary>
    /// Loads a sound effect from the specified file path for short audio clips like UI sounds and game effects.
    /// Sound effects can be played multiple times simultaneously.
    /// </summary>
    /// <param name="path">Relative path to the sound file from the content root directory</param>
    /// <param name="soundName">Unique name to identify and retrieve the loaded sound effect</param>
    /// <exception cref="FileNotFoundException">Thrown when the sound file is not found at the specified path</exception>
    public void LoadSound(string path, string soundName)
    {
        _logger.Debug("Loading sound: {Path} - {SoundName}", path, soundName);
        var soundPath = Path.Combine(_rootDirectory, path);

        if (!File.Exists(soundPath))
        {
            _logger.Error("Sound file not found: {Path}", soundPath);
            throw new FileNotFoundException($"Sound file not found: {soundPath}");
        }

        using var fileStream = File.OpenRead(soundPath);
        LoadSoundFromStream(fileStream, soundName);

        _logger.Information("Sound loaded: {SoundName} from {Path}", soundName, path);
    }

    /// <summary>
    /// Loads a sound effect from a stream for short audio clips like UI sounds and game effects.
    /// Useful for loading sounds from embedded resources or network streams.
    /// </summary>
    /// <param name="stream">Stream containing the sound data</param>
    /// <param name="soundName">Unique name to identify and retrieve the loaded sound effect</param>
    public void LoadSoundFromStream(Stream stream, string soundName)
    {
        _logger.Debug("Loading sound from stream: {SoundName}", soundName);

        var soundEffect = SoundEffect.FromStream(stream);
        _loadedSounds[soundName] = soundEffect;

        _logger.Information("Sound loaded from stream: {SoundName}", soundName);
    }

    /// <summary>
    /// Loads a music track from the specified file path for background music and longer audio content.
    /// Only one music track can be played at a time.
    /// </summary>
    /// <param name="path">Relative path to the music file from the content root directory</param>
    /// <param name="musicName">Unique name to identify and retrieve the loaded music track</param>
    /// <exception cref="FileNotFoundException">Thrown when the music file is not found at the specified path</exception>
    public void LoadMusic(string path, string musicName)
    {
        _logger.Debug("Loading music: {Path} - {MusicName}", path, musicName);
        var musicPath = Path.Combine(_rootDirectory, path);

        if (!File.Exists(musicPath))
        {
            _logger.Error("Music file not found: {Path}", musicPath);
            throw new FileNotFoundException($"Music file not found: {musicPath}");
        }

        var song = Song.FromUri(musicName, new Uri(musicPath, UriKind.Absolute));
        _loadedMusic[musicName] = song;

        _logger.Information("Music loaded: {MusicName} from {Path}", musicName, path);
    }

    /// <summary>
    /// Loads a music track from a stream for background music and longer audio content.
    /// Useful for loading music from embedded resources or network streams.
    /// </summary>
    /// <param name="stream">Stream containing the music data</param>
    /// <param name="musicName">Unique name to identify and retrieve the loaded music track</param>
    public void LoadMusicFromStream(Stream stream, string musicName)
    {
        _logger.Debug("Loading music from stream: {MusicName}", musicName);

        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempPath))
            {
                stream.CopyTo(fileStream);
            }

            var song = Song.FromUri(musicName, new Uri(tempPath, UriKind.Absolute));
            _loadedMusic[musicName] = song;

            _logger.Information("Music loaded from stream: {MusicName}", musicName);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Retrieves a previously loaded sound effect by name for audio playback.
    /// Sound effects can be played multiple times simultaneously for overlapping audio.
    /// </summary>
    /// <param name="soundName">Name of the sound effect as registered during loading</param>
    /// <returns>SoundEffect instance ready for immediate playback</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the sound name is not found in the cache</exception>
    public SoundEffect GetSound(string soundName)
    {
        if (!_loadedSounds.TryGetValue(soundName, out var soundEffect))
        {
            _logger.Error("Sound not found: {SoundName}", soundName);
            throw new KeyNotFoundException($"Sound '{soundName}' not found. Load this sound first.");
        }

        _logger.Debug("Retrieved sound: {SoundName}", soundName);
        return soundEffect;
    }

    /// <summary>
    /// Retrieves a previously loaded music track by name for background music playback.
    /// Only one music track can be played at a time through the MediaPlayer.
    /// </summary>
    /// <param name="musicName">Name of the music track as registered during loading</param>
    /// <returns>Song instance ready for background music playback</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the music name is not found in the cache</exception>
    public Song GetMusic(string musicName)
    {
        if (!_loadedMusic.TryGetValue(musicName, out var song))
        {
            _logger.Error("Music not found: {MusicName}", musicName);
            throw new KeyNotFoundException($"Music '{musicName}' not found. Load this music first.");
        }

        _logger.Debug("Retrieved music: {MusicName}", musicName);
        return song;
    }

    /// <summary>
    /// Helper method to extract a file from a ZIP archive and return it as a stream.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP archive file</param>
    /// <param name="assetPath">Internal path of the file within the ZIP archive</param>
    /// <returns>Stream containing the extracted file data</returns>
    /// <exception cref="FileNotFoundException">Thrown when the ZIP file is not found</exception>
    /// <exception cref="InvalidDataException">Thrown when the asset is not found in the ZIP archive</exception>
    private Stream ExtractFileFromZip(string zipPath, string assetPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"ZIP file not found: {zipPath}");
        }

        using var zipArchive = ZipFile.OpenRead(zipPath);
        var entry = zipArchive.GetEntry(assetPath);

        if (entry == null)
        {
            throw new InvalidDataException($"Asset not found in ZIP archive: {assetPath}");
        }

        var memoryStream = new MemoryStream();
        using var entryStream = entry.Open();
        entryStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }
}
