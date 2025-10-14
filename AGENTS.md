# AGENTS.md

This file documents custom commands and agents for the project.

## /format

Command: `/format`

Description: Searches all .cs files in the project. For each file that contains exactly one type (class, record, struct, interface, enum, etc.), checks if the type has XML documentation comments (///). If not, analyzes the context of the type and adds appropriate XML documentation comments above the type. If the file contains the token '##REFORMAT##', removes existing XML documentation comments and regenerates them from scratch.

This ensures that single-type files have meaningful documentation based on their purpose and structure.

## /oco

Command: `/oco`

Description: Analyzes the current git changes, generates a conventional commit message, commits the changes, and pushes to the remote repository.

---

# SquidCraft Client Architecture

## 3D Rendering System

### Components

#### CameraComponent (`Components/CameraComponent.cs`)
First-person 3D camera with full input handling.

**Properties:**
- `Position` - Camera world position (Vector3)
- `Target` - Look-at target (Vector3)
- `FieldOfView` - FOV in radians (default: π/4)
- `NearPlane` / `FarPlane` - Clipping planes
- `MoveSpeed` - Movement speed (default: 20f)
- `MouseSensitivity` - Mouse look sensitivity (default: 0.003f)
- `EnableInput` - Enable/disable input handling

**Controls:**
- `W/A/S/D` - Movement (forward/left/back/right)
- `Space` - Move up
- `Left Shift` - Move down
- `Mouse` - Look around (yaw/pitch)

**Key Methods:**
- `GetPickRay()` - Ray from camera forward for raycasting
- `GetPickRay(screenX, screenY)` - Ray from screen coordinates
- `LookAt(position, target)` - Set camera position and target
- `Move(delta)` - Translate camera
- `Rotate(yaw, pitch)` - Rotate camera

**Usage:**
```csharp
var camera = new CameraComponent(GraphicsDevice)
{
    Position = new Vector3(8, 66, 8),
    MoveSpeed = 25f,
    MouseSensitivity = 0.003f,
    EnableInput = true
};
```

---

#### ChunkComponent (`Components/ChunkComponent.cs`)
Renders a single 16x64x16 chunk with greedy meshing and async mesh building.

**Key Features:**
- **Greedy meshing** - Skips internal faces between solid blocks
- **Cross-chunk culling** - Skips faces at chunk boundaries
- **Async mesh building** - Background thread for vertex generation
- **Vertex colors** - Per-face lighting/AO support
- **Custom block heights** - Support for water, slabs, etc.
- **Fade-in animation** - Smooth chunk appearance
- **Transparency support** - Alpha blending for water/glass

**Properties:**
- `Chunk` - The ChunkEntity data
- `Position` - World position (Vector3)
- `AutoRotate` - Enable auto-rotation (debug)
- `BlockScale` - Scale factor for blocks
- `RenderTransparentBlocks` - Enable transparency rendering
- `Opacity` - Current opacity (0-1) for fade-in
- `FadeInSpeed` - Fade animation speed (default: 2f)
- `EnableFadeIn` - Enable fade-in animation
- `GetNeighborChunk` - Delegate for cross-chunk culling

**Mesh Building:**
```
BuildMeshImmediate()
├── Task.Run(BuildMeshData)  ← Background thread
│   ├── Loop 16x64x16 blocks
│   ├── Face culling checks
│   ├── Generate vertices & indices
│   └── Return MeshData
└── Main thread continues rendering

CheckMeshBuildCompletion() (in Update)
├── Check if task completed
└── UploadMeshToGpu() ← Main thread only
    ├── Create VertexBuffer
    ├── Create IndexBuffer
    └── ~2ms per chunk
```

**Vertex Format:**
```csharp
VertexPositionColorTexture
├── Position - Block vertex position
├── Color - Lighting/AO (per-face)
└── TextureCoordinate - Atlas UV
```

**Face Culling Logic:**
```csharp
ShouldRenderFace(x, y, z, side):
├── Get neighbor block
├── If Air → Render
├── If same BlockType + IsLiquid → Don't render (water-to-water)
├── If transparent → Render (see through)
└── Else → Don't render (solid-to-solid)
```

**Usage:**
```csharp
var chunk = new ChunkComponent
{
    AutoRotate = false,
    BlockScale = 1f,
    RenderTransparentBlocks = true,
    EnableFadeIn = true,
    FadeInSpeed = 2f,
    GetNeighborChunk = worldComponent.GetChunkEntity
};
chunk.SetChunk(chunkEntity);
```

---

#### WorldComponent (`Components/WorldComponent.cs`)
Manages multiple chunks with dynamic loading/unloading and frustum culling.

**Key Features:**
- **Dynamic chunk loading** - Load chunks as player moves
- **View range culling** - Only render nearby chunks
- **Frustum culling** - Skip chunks outside camera view
- **Generation range** - Pre-load chunks before visible
- **Async server integration** - Request chunks from network
- **Deferred mesh building** - Build N meshes per frame
- **Block raycasting** - Pick blocks with mouse
- **Cross-chunk face culling** - Seamless chunk borders

**Properties:**
```csharp
ViewRange = 150f          // Render distance
GenerationRange = 200f    // Pre-load distance
ChunkLoadDistance = 2     // Visible chunks grid (5x5)
GenerationDistance = 3    // Pre-load chunks grid (7x7)
MaxChunkBuildsPerFrame = 5 // Mesh builds per frame
EnableFrustumCulling = true
MaxRaycastDistance = 10f
```

**Delegates:**
```csharp
// Local/synchronous generation
public delegate ChunkEntity ChunkGeneratorDelegate(int chunkX, int chunkZ);
public ChunkGeneratorDelegate? ChunkGenerator { get; set; }

// Server/network generation
public delegate Task<ChunkEntity> ChunkGeneratorAsyncDelegate(int chunkX, int chunkZ);
public ChunkGeneratorAsyncDelegate? ChunkGeneratorAsync { get; set; }
```

**Chunk Loading Flow:**
```
Player moves to new chunk:
├── LoadChunksAroundPlayer(centerX, centerZ)
│   ├── Loop GenerationDistance (7x7 = 49 chunks)
│   ├── For each missing chunk:
│   │   ├── If ChunkGeneratorAsync:
│   │   │   └── RequestChunkFromServerAsync(x, z)
│   │   └── Else if ChunkGenerator:
│   │       └── Generate locally
│   └── Invalidate neighbor chunk meshes
│
└── UnloadDistantChunks(centerX, centerZ)
    └── Remove chunks beyond GenerationDistance+1
```

**Mesh Building Queue:**
```
ProcessMeshBuildQueue() (every frame):
├── Dequeue up to MaxChunkBuildsPerFrame chunks
├── Call chunk.BuildMeshImmediate()
│   └── Starts background Task
└── Main thread continues (no blocking!)
```

**View Range Culling:**
```
ShouldRenderChunk(chunk, cameraPos):
├── Calculate distance to chunk center
├── If distance > ViewRange → Skip
├── If EnableFrustumCulling:
│   ├── Create BoundingSphere for chunk
│   └── Test frustum.Contains(sphere)
│       └── If Disjoint → Skip
└── Render chunk
```

**Block Raycasting:**
```
RaycastBlock(ray):
├── Step along ray (0.1 units)
├── For each point:
│   ├── Find containing chunk
│   ├── Convert to local coords
│   └── Check if solid block
└── Return (Chunk, X, Y, Z) or null
```

**Usage:**
```csharp
var world = new WorldComponent(GraphicsDevice, camera)
{
    ViewRange = 150f,
    GenerationRange = 200f,
    ChunkLoadDistance = 2,
    GenerationDistance = 3,
    MaxChunkBuildsPerFrame = 5,
    ChunkGenerator = CreateFlatChunk,
    // OR for server:
    ChunkGeneratorAsync = serverProvider.RequestChunkAsync
};
```

**Server Integration Example:**
```csharp
public class ServerChunkProvider
{
    public async Task<ChunkEntity> RequestChunkAsync(int x, int z)
    {
        // Send request to server
        var request = new ChunkRequestMessage { X = x, Z = z };
        await _networkClient.SendAsync(request);
        
        // Wait for response
        var response = await _networkClient.WaitForChunkAsync(x, z);
        return response.Chunk;
    }
}

// In Game1.cs:
_worldComponent.ChunkGeneratorAsync = serverProvider.RequestChunkAsync;
```

---

#### BlockOutlineComponent (`Components/BlockOutlineComponent.cs`)
Renders white outline around selected block.

**Features:**
- 12 lines forming cube wireframe
- 0.005 offset to prevent z-fighting
- Uses `DepthStencilState.DepthRead`
- Configurable color and line width

**Usage:**
```csharp
var outline = new BlockOutlineComponent(GraphicsDevice)
{
    OutlineColor = Color.White * 0.8f
};

// In draw loop:
if (worldComponent.SelectedBlock is var selected && selected.HasValue)
{
    var (chunk, x, y, z) = selected.Value;
    var worldPos = chunk.Position + new Vector3(x, y, z);
    outline.Draw(worldPos, camera.View, camera.Projection);
}
```

---

## Block System

### BlockDefinitionData (`Game.Data/Assets/BlockDefinitionData.cs`)

**Properties:**
- `BlockType` - Enum type (Grass, Dirt, Water, etc.)
- `Sides` - Dictionary<SideType, string> (texture atlas indices)
- `IsTransparent` - Alpha blending enabled
- `IsLiquid` - Special liquid culling (water-to-water)
- `IsSolid` - Collision/physics
- `Height` - Custom height (0.0-1.0, default: 1.0)

**blocks.json Example:**
```json
{
  "BlockType": "Water",
  "IsTransparent": true,
  "IsLiquid": true,
  "IsSolid": false,
  "Height": 0.875,
  "Sides": {
    "Top": "2",
    "Bottom": "2",
    "North": "2",
    "South": "2",
    "East": "2",
    "West": "2"
  }
}
```

### Vertex Color Lighting

**Per-Face Ambient Occlusion:**
```csharp
CalculateFaceColor(x, y, z, side):
├── Top:    100% brightness (full sun)
├── Bottom:  50% brightness (shadow)
├── North/South: 80% brightness
└── East/West:   75% brightness
```

**Customization Examples:**
```csharp
// Height-based lighting
var heightFactor = y / (float)ChunkEntity.Height;
var brightness = baseLight * (0.5f + heightFactor * 0.5f);
return new Color(brightness, brightness, brightness);

// Time-of-day
var dayNightFactor = MathF.Cos(timeOfDay * MathF.PI);
var brightness = baseBrightness * dayNightFactor;
```

---

## Performance Optimization

### Async Mesh Building

**Problem:** Building chunk mesh blocks main thread (~50ms) = frame drops

**Solution:** Split CPU work (background) from GPU upload (main thread)

```
Timeline:
Frame 1: Player moves → 49 chunks requested
Frame 2-50: Server responds (async)
Frame 51: 5× Task.Run(BuildMeshData) in background
Frame 52: UploadMeshToGpu() ~10ms total (smooth!)
Frame 53-70: Continue building (5/frame)
Frame 71+: All chunks ready + fade-in
```

**Configuration:**
- `MaxChunkBuildsPerFrame = 5` - Higher = faster loading
- Increase if GPU powerful, decrease if CPU limited

### Deferred Mesh Building Queue

Prevents building all chunks at once:
```
_meshBuildQueue (Queue<ChunkComponent>)
├── ProcessMeshBuildQueue() every frame
├── Build MAX N chunks per frame
└── Smooth 60 FPS maintained
```

### Frustum Culling

Skip rendering chunks outside camera view:
```
Performance gain:
├── 7x7 grid = 49 chunks total
├── Visible ~20-25 chunks
└── ~50% reduction in draw calls
```

### Cross-Chunk Face Culling

Skip faces between adjacent chunks:
```
Before: 100 faces for 5x5 water pool
After:  ~20 faces (only perimeter)
Result: 80% triangle reduction
```

### View Range vs Generation Range

```
ViewRange = 150f      // Render nearby chunks
GenerationRange = 200f // Pre-load farther chunks

Benefits:
├── Chunks ready before entering ViewRange
├── No pop-in when moving
└── Smooth exploration experience
```

---

## System Requirements

### Threading Model
- **Main thread:** Rendering, GPU upload, input, game logic
- **Background threads:** Mesh building, chunk generation
- **Network thread:** Server communication (if async delegate)

### Performance Targets
- **60 FPS** with 25-49 chunks loaded
- **~2ms** per chunk GPU upload
- **~10ms** max per frame for mesh building
- **Smooth** chunk loading with fade-in

### Memory
- ~500KB per chunk (vertex/index buffers)
- 49 chunks × 500KB = ~25MB for 7x7 grid
- Scales linearly with GenerationDistance

---

## Usage Examples

### Basic Setup

```csharp
// Camera
var camera = new CameraComponent(GraphicsDevice)
{
    Position = new Vector3(8, 66, 8),
    Target = new Vector3(8, 66, 24),
    MoveSpeed = 25f,
    EnableInput = true
};

// World
var world = new WorldComponent(GraphicsDevice, camera)
{
    ViewRange = 150f,
    GenerationRange = 200f,
    ChunkLoadDistance = 2,
    GenerationDistance = 3,
    MaxChunkBuildsPerFrame = 5,
    ChunkGenerator = (x, z) => CreateFlatChunk(x, z)
};

// Block outline
var outline = new BlockOutlineComponent(GraphicsDevice);

// Update loop
world.Update(gameTime);

// Draw loop
world.Draw(gameTime);
if (world.SelectedBlock.HasValue)
{
    var (chunk, x, y, z) = world.SelectedBlock.Value;
    var pos = chunk.Position + new Vector3(x, y, z);
    outline.Draw(pos, camera.View, camera.Projection);
}
```

### Server Integration

```csharp
public class NetworkChunkService
{
    private readonly NetworkClient _client;
    
    public async Task<ChunkEntity> RequestChunkAsync(int x, int z)
    {
        var request = new ChunkRequestMessage { X = x, Z = z };
        await _client.SendAsync(request);
        
        var response = await _client.WaitForChunkAsync(x, z, 
            timeout: TimeSpan.FromSeconds(5));
        
        return response.Chunk;
    }
}

// Setup
var networkService = new NetworkChunkService(client);
_worldComponent.ChunkGeneratorAsync = networkService.RequestChunkAsync;
```

---

## Tips & Best Practices

1. **Always use ChunkGeneratorAsync** for network/server chunks
2. **Set GenerationRange > ViewRange** for smooth pre-loading
3. **Adjust MaxChunkBuildsPerFrame** based on hardware
4. **Enable frustum culling** for large view distances
5. **Use EnableFadeIn** for polished chunk appearance
6. **Monitor mesh build queue** length in logs
7. **Test with high ChunkLoadDistance** for performance
8. **Profile GPU upload time** if stuttering persists

---

## Troubleshooting

**Frame drops when loading chunks:**
- Increase `MaxChunkBuildsPerFrame` (if GPU strong)
- Decrease `GenerationDistance` (fewer chunks)
- Check mesh build queue isn't backing up

**Chunks pop in suddenly:**
- Increase `GenerationRange` relative to `ViewRange`
- Enable `EnableFadeIn` on chunks
- Increase `FadeInSpeed` for faster fade

**Seams between chunks:**
- Ensure `GetNeighborChunk` delegate is set
- Check cross-chunk culling is working
- Verify chunk positions are aligned to grid

**Invisible blocks:**
- Check `RenderTransparentBlocks` is true
- Verify block definitions have textures
- Check face culling logic

**Low FPS:**
- Reduce `ViewRange`
- Enable `EnableFrustumCulling`
- Lower `GenerationDistance`
- Increase `MaxChunkBuildsPerFrame` to clear queue faster