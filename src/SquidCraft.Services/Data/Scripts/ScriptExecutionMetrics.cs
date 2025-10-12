namespace SquidCraft.Services.Data.Scripts;

/// <summary>
///     Metrics about script execution performance
/// </summary>
public class ScriptExecutionMetrics
{
    public long ExecutionTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int StatementsExecuted { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public int TotalScriptsCached { get; set; }
}
