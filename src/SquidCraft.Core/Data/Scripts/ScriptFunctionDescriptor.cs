namespace SquidCraft.Core.Data.Scripts;

/// <summary>
///     Descriptor class containing metadata about a script function for documentation generation
/// </summary>
public class ScriptFunctionDescriptor
{
    public string ModuleName { get; set; }
    public string FunctionName { get; set; }
    public string? Help { get; set; }

    public List<ScriptFunctionParameterDescriptor> Parameters { get; set; } = new();
    public string ReturnType { get; set; }

    public Type RawReturnType { get; set; } = null!;

    public override string ToString()
    {
        return
            $"{FunctionName}({string.Join(", ", Parameters.Select(p => $"{p.ParameterType} {p.ParameterName}"))}) : {ReturnType}";
    }
}
