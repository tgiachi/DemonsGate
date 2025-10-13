namespace SquidCraft.Network.Types;

/// <summary>
///     Defines the types of network messages supported by the SquidCraft protocol
/// </summary>
public enum NetworkMessageType : byte
{
    /// <summary>Ping message to check connection status</summary>
    Ping,

    /// <summary>Pong message in response to a ping</summary>
    Pong,

    /// <summary>Login request message for user authentication</summary>
    LoginRequest,

    /// <summary>Login response message indicating success or failure</summary>
    LoginResponse,

    /// <summary>System-level chat message broadcast to connected clients</summary>
    SystemChat,

    /// <summary>Request for server version information</summary>
    VersionRequest,

    /// <summary>Response containing server version details</summary>
    VersionResponse,

    /// <summary>Request for a specific game asset (texture, model, etc.)</summary>
    AssetRequest,

    /// <summary>Response containing the requested asset data</summary>
    AssetResponse,

    /// <summary>Request for the list of available assets on the server</summary>
    AssetListRequest,

    /// <summary>Response containing the list of available assets</summary>
    AssetListResponse,

    PlayerPositionRequest,
    PlayerPositionResponse,


    ChunkRequest,
    ChunkResponse,
}
