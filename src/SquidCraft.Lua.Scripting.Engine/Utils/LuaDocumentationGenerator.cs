using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SquidCraft.Core.Attributes.Scripts;
using SquidCraft.Core.Data.Scripts;
using SquidCraft.Core.Extensions.Strings;

namespace SquidCraft.Lua.Scripting.Engine.Utils;

/// <summary>
///     Utility class for generating Lua meta files with EmmyLua/LuaLS annotations
///     Automatically creates meta.lua files with function signatures, types, and documentation
/// </summary>
[RequiresUnreferencedCode(
    "This class uses reflection to analyze types for Lua meta generation and requires full type metadata."
)]
public static class LuaDocumentationGenerator
{
    private static readonly HashSet<Type> _processedTypes = new();
    private static readonly StringBuilder _classesBuilder = new();
    private static readonly StringBuilder _constantsBuilder = new();
    private static readonly StringBuilder _enumsBuilder = new();
    private static readonly List<Type> _classTypesToGenerate = new(32);
    private static readonly Dictionary<Type, bool> _recordTypeCache = new();

    private static Func<string, string> _nameResolver = name => name.ToSnakeCase();

    public static List<Type> FoundEnums { get; } = new(16);

    public static void AddClassToGenerate(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _classTypesToGenerate.Add(type);
    }

    /// <summary>
    ///     Clears all internal caches and state
    /// </summary>
    public static void ClearCaches()
    {
        _processedTypes.Clear();
        _recordTypeCache.Clear();
        _classTypesToGenerate.Clear();
        FoundEnums.Clear();
        _classesBuilder.Clear();
        _constantsBuilder.Clear();
        _enumsBuilder.Clear();
    }

    [SuppressMessage("Trimming", "IL2075:Reflection", Justification = "Reflection is required for script module analysis")]
    [SuppressMessage(
        "Trimming",
        "IL2072:Reflection",
        Justification = "Reflection is required for parameter and return type analysis"
    )]
    public static string GenerateDocumentation(
        string appName, string appVersion, List<ScriptModuleData> scriptModules, Dictionary<string, object> constants,
        Func<string, string>? nameResolver = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentException.ThrowIfNullOrWhiteSpace(appVersion);
        ArgumentNullException.ThrowIfNull(scriptModules);
        ArgumentNullException.ThrowIfNull(constants);

        if (nameResolver != null)
        {
            _nameResolver = nameResolver;
        }

        var sb = new StringBuilder();
        sb.AppendLine("---@meta");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--- {appName} v{appVersion} Lua API");
        sb.AppendLine(CultureInfo.InvariantCulture, $"--- Auto-generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();

        // Reset processed types and builders
        _processedTypes.Clear();
        _classesBuilder.Clear();
        _constantsBuilder.Clear();
        _enumsBuilder.Clear();
        _classTypesToGenerate.Clear();

        var distinctConstants = constants
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.First().Value);

        ProcessConstants(distinctConstants);
        sb.Append(_constantsBuilder);

        foreach (var module in scriptModules)
        {
            var scriptModuleAttribute = module.ModuleType.GetCustomAttribute<ScriptModuleAttribute>();

            if (scriptModuleAttribute is null)
            {
                continue;
            }

            var moduleName = scriptModuleAttribute.Name;
            var moduleHelpText = scriptModuleAttribute.HelpText;

            sb.AppendLine("---");
            sb.AppendLine(CultureInfo.InvariantCulture, $"--- {module.ModuleType.Name} module");

            if (!string.IsNullOrWhiteSpace(moduleHelpText))
            {
                sb.AppendLine("---");
                sb.AppendLine(CultureInfo.InvariantCulture, $"--- {moduleHelpText}");
            }

            sb.AppendLine("---");
            sb.AppendLine(CultureInfo.InvariantCulture, $"---@class {moduleName}");

            // Get all methods with ScriptFunction attribute
            var methods = module.ModuleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<ScriptFunctionAttribute>() is not null);

            foreach (var method in methods)
            {
                var scriptFunctionAttr = method.GetCustomAttribute<ScriptFunctionAttribute>();

                if (scriptFunctionAttr is null)
                {
                    continue;
                }

                var functionName = string.IsNullOrWhiteSpace(scriptFunctionAttr.FunctionName)
                    ? _nameResolver(method.Name)
                    : scriptFunctionAttr.FunctionName;
                var description = scriptFunctionAttr.HelpText ?? "No description available";

                sb.AppendLine(CultureInfo.InvariantCulture, $"{moduleName}.{functionName} = function() end");
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"{moduleName} = {{}}");
            sb.AppendLine();

            // Now generate detailed function documentation
            foreach (var method in methods)
            {
                var scriptFunctionAttr = method.GetCustomAttribute<ScriptFunctionAttribute>();

                if (scriptFunctionAttr is null)
                {
                    continue;
                }

                var functionName = string.IsNullOrWhiteSpace(scriptFunctionAttr.FunctionName)
                    ? _nameResolver(method.Name)
                    : scriptFunctionAttr.FunctionName;
                var description = scriptFunctionAttr.HelpText ?? "No description available";

                sb.AppendLine("---");
                sb.AppendLine(CultureInfo.InvariantCulture, $"--- {description}");
                sb.AppendLine("---");

                // Add parameter documentation
                var parameters = method.GetParameters();
                foreach (var param in parameters)
                {
                    var paramType = ConvertToLuaType(param.ParameterType);
                    var paramName = param.Name ?? $"param{Array.IndexOf(parameters, param)}";
                    var paramDescription = GetParameterDescription(param, paramType);
                    sb.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"---@param {_nameResolver(paramName)} {paramType} {paramDescription}"
                    );
                }

                // Add return type documentation
                if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
                {
                    var returnType = ConvertToLuaType(method.ReturnType);
                    var returnDescription = GetReturnDescription(method.ReturnType, returnType);
                    sb.AppendLine(CultureInfo.InvariantCulture, $"---@return {returnType} {returnDescription}");
                }

                // Function signature
                sb.Append(CultureInfo.InvariantCulture, $"function {moduleName}.{functionName}(");

                for (var i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramName = param.Name ?? $"param{i}";
                    sb.Append(_nameResolver(paramName));

                    if (i < parameters.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }

                sb.AppendLine(") end");
                sb.AppendLine();
            }
        }

        // Generate all classes that were collected during type conversion
        GenerateAllClasses();

        // Append enums and classes
        sb.Append(_enumsBuilder);
        sb.AppendLine();
        sb.Append(_classesBuilder);

        return sb.ToString();
    }

    /// <summary>
    ///     Generate all classes after collecting them
    /// </summary>
    private static void GenerateAllClasses()
    {
        var processedInIteration = new HashSet<Type>();
        var remainingTypes = new List<Type>(_classTypesToGenerate);

        while (remainingTypes.Count > 0)
        {
            processedInIteration.Clear();

            for (var i = remainingTypes.Count - 1; i >= 0; i--)
            {
                var type = remainingTypes[i];

                if (_processedTypes.Contains(type))
                {
                    remainingTypes.RemoveAt(i);
                    continue;
                }

                if (CanProcessType(type))
                {
                    GenerateClass(type);
                    processedInIteration.Add(type);
                    remainingTypes.RemoveAt(i);
                }
            }

            if (processedInIteration.Count == 0 && remainingTypes.Count > 0)
            {
                foreach (var type in remainingTypes)
                {
                    if (!_processedTypes.Contains(type))
                    {
                        GenerateClass(type);
                    }
                }

                break;
            }
        }
    }

    private static bool CanProcessType(Type type)
    {
        return true;
    }

    /// <summary>
    ///     Check if a type is a C# record type
    /// </summary>
    [SuppressMessage("Trimming", "IL2070:Reflection", Justification = "Reflection is required for record type detection")]
    private static bool IsRecordType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods
        )]
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_recordTypeCache.TryGetValue(type, out var isRecord))
        {
            return isRecord;
        }

        if (!type.IsClass)
        {
            _recordTypeCache[type] = false;
            return false;
        }

        var equalityContract = type.GetProperty(
            "EqualityContract",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (equalityContract is not null && equalityContract.PropertyType == typeof(Type))
        {
            _recordTypeCache[type] = true;
            return true;
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var hasCompilerGeneratedToString = methods.Any(m =>
            m.Name == "ToString" &&
            m.GetParameters().Length == 0 &&
            m.GetCustomAttributes().Any(attr => attr.GetType().Name.Contains("CompilerGenerated"))
        );

        var result = hasCompilerGeneratedToString;
        _recordTypeCache[type] = result;
        return result;
    }

    /// <summary>
    ///     Generate a single class
    /// </summary>
    [SuppressMessage("Trimming", "IL2070:Reflection", Justification = "Reflection is required for class generation")]
    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for class generation")]
    private static void GenerateClass(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors
        )]
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!_processedTypes.Add(type))
        {
            return;
        }

        var className = type.Name;

        _classesBuilder.AppendLine();
        _classesBuilder.AppendLine("---");

        if (IsRecordType(type))
        {
            _classesBuilder.AppendLine(CultureInfo.InvariantCulture, $"--- Record type {type.FullName}");
        }
        else
        {
            _classesBuilder.AppendLine(CultureInfo.InvariantCulture, $"--- Class {type.FullName}");
        }

        _classesBuilder.AppendLine("---");
        _classesBuilder.AppendLine(CultureInfo.InvariantCulture, $"---@class {className}");

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            if (string.IsNullOrEmpty(propertyName))
            {
                continue;
            }

            var propertyType = ConvertToLuaType(property.PropertyType);
            _classesBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"---@field {_nameResolver(propertyName)} {propertyType}"
            );
        }

        _classesBuilder.AppendLine();
    }

    [SuppressMessage("Trimming", "IL2070:Reflection", Justification = "Reflection is required for Lua type conversion")]
    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for Lua type conversion")]
    [SuppressMessage("Trimming", "IL2062:Reflection", Justification = "Reflection is required for Lua type conversion")]
    private static string ConvertToLuaType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicConstructors
        )]
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        // Handle void
        if (type == typeof(void))
        {
            return "nil";
        }

        // Handle string
        if (type == typeof(string))
        {
            return "string";
        }

        // Handle numbers
        if (type == typeof(int) || type == typeof(long) || type == typeof(float) ||
            type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
            type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
            type == typeof(byte) || type == typeof(sbyte))
        {
            return "number";
        }

        // Handle boolean
        if (type == typeof(bool))
        {
            return "boolean";
        }

        // Handle object
        if (type == typeof(object))
        {
            return "any";
        }

        // Handle Task (async)
        if (type == typeof(Task))
        {
            return "nil";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = type.GetGenericArguments()[0];
            return ConvertToLuaType(taskResultType);
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is null)
            {
                return "table";
            }

            return $"{ConvertToLuaType(elementType)}[]";
        }

        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is null)
            {
                return "any";
            }

            return $"{ConvertToLuaType(underlyingType)}?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            // Handle Dictionary
            if (genericTypeDefinition == typeof(Dictionary<,>))
            {
                var keyType = ConvertToLuaType(genericArgs[0]);
                var valueType = ConvertToLuaType(genericArgs[1]);
                return $"table<{keyType}, {valueType}>";
            }

            // Handle List, IEnumerable, etc.
            if (genericTypeDefinition == typeof(List<>) ||
                genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IList<>))
            {
                return $"{ConvertToLuaType(genericArgs[0])}[]";
            }

            // Handle Action delegates
            if (genericTypeDefinition == typeof(Action) ||
                genericTypeDefinition.Name.StartsWith("Action`", StringComparison.Ordinal))
            {
                return "fun()";
            }

            // Handle Func delegates
            if (genericTypeDefinition.Name.StartsWith("Func`", StringComparison.Ordinal))
            {
                var returnType = ConvertToLuaType(genericArgs[^1]);
                return $"fun():{returnType}";
            }
        }

        // Handle enums
        if (type.IsEnum)
        {
            GenerateEnumClass(type);
            return _nameResolver(type.Name);
        }

        // Handle record types
        if (IsRecordType(type))
        {
            var className = type.Name;

            if (_processedTypes.Contains(type))
            {
                return className;
            }

            if (!_classTypesToGenerate.Contains(type))
            {
                _classTypesToGenerate.Add(type);
            }

            return className;
        }

        // Handle other complex types
        if ((type.IsClass || type.IsValueType) && !type.IsPrimitive &&
            type.Namespace is not null && !type.Namespace.StartsWith("System", StringComparison.Ordinal))
        {
            var className = type.Name;

            if (_processedTypes.Contains(type))
            {
                return className;
            }

            if (!_classTypesToGenerate.Contains(type))
            {
                _classTypesToGenerate.Add(type);
            }

            return className;
        }

        // Handle delegates
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return "function";
        }

        return "any";
    }

    [SuppressMessage(
        "Trimming",
        "IL2072:Reflection",
        Justification = "Reflection is required for constant value formatting"
    )]
    private static string FormatConstantValue(object? value, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (value is null)
        {
            return "nil";
        }

        if (type == typeof(string))
        {
            return $"\"{value}\"";
        }

        if (type == typeof(bool))
        {
            return value.ToString()!.ToLowerInvariant();
        }

        if (type.IsEnum)
        {
            return $"{_nameResolver(type.Name)}.{value}";
        }

        return value.ToString() ?? "nil";
    }

    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for constant type analysis")]
    private static void ProcessConstants(Dictionary<string, object> constants)
    {
        ArgumentNullException.ThrowIfNull(constants);

        if (constants.Count == 0)
        {
            return;
        }

        _constantsBuilder.AppendLine("--- Global constants");
        _constantsBuilder.AppendLine();

        foreach (var constant in constants)
        {
            var constantName = constant.Key ?? "unnamed";
            var constantValue = constant.Value;
            var constantType = constantValue?.GetType() ?? typeof(object);

            var luaType = ConvertToLuaType(constantType);
            var formattedValue = FormatConstantValue(constantValue, constantType);

            _constantsBuilder.AppendLine("---");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"--- {constantName} constant");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"--- Value: {formattedValue}");
            _constantsBuilder.AppendLine("---");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"---@type {luaType}");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"{constantName} = {formattedValue}");
            _constantsBuilder.AppendLine();
        }

        _constantsBuilder.AppendLine();
    }

    [SuppressMessage(
        "Trimming",
        "IL2070:Reflection",
        Justification = "Reflection is required for enum class generation"
    )]
    private static void GenerateEnumClass(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Type must be an enum", nameof(enumType));
        }

        if (!_processedTypes.Add(enumType))
        {
            return;
        }

        FoundEnums.Add(enumType);

        _enumsBuilder.AppendLine();
        _enumsBuilder.AppendLine("---");
        _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"--- Enum {enumType.FullName}");
        _enumsBuilder.AppendLine("---");
        _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"---@class {_nameResolver(enumType.Name)}");

        var enumValues = Enum.GetNames(enumType);
        var enumUnderlyingType = Enum.GetUnderlyingType(enumType);

        foreach (var value in enumValues)
        {
            try
            {
                var enumValue = Enum.Parse(enumType, value);
                var numericValue = Convert.ChangeType(enumValue, enumUnderlyingType, CultureInfo.InvariantCulture);
                _enumsBuilder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"---@field {value} number # Value: {numericValue}"
                );
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException)
            {
                _enumsBuilder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"---@field {value} number # Unable to determine value"
                );
            }
        }

        _enumsBuilder.AppendLine();
        _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"{_nameResolver(enumType.Name)} = {{}}");
        _enumsBuilder.AppendLine();
    }

    /// <summary>
    ///     Gets enhanced parameter description with type information
    /// </summary>
    private static string GetParameterDescription(ParameterInfo param, string luaType)
    {
        var baseName = param.Name ?? "parameter";
        var description = $"The {baseName.ToLowerInvariant()}";

        if (luaType.Contains("number"))
        {
            description += " value";
        }
        else if (luaType.Contains("string"))
        {
            description += " text";
        }
        else if (luaType.Contains("boolean"))
        {
            description += " flag";
        }
        else if (luaType.Contains("[]") || luaType.Contains("table"))
        {
            description += " table";
        }
        else
        {
            description += $" of type {luaType}";
        }

        if (param.IsOptional)
        {
            description += " (optional)";
        }

        return description;
    }

    /// <summary>
    ///     Gets enhanced return description with type information
    /// </summary>
    private static string GetReturnDescription(Type returnType, string luaType)
    {
        var description = "The ";

        if (luaType.Contains("number"))
        {
            description += "computed numeric value";
        }
        else if (luaType.Contains("string"))
        {
            description += "resulting text";
        }
        else if (luaType.Contains("boolean"))
        {
            description += "result of the operation";
        }
        else if (luaType.Contains("[]") || luaType.Contains("table"))
        {
            description += "collection of results";
        }
        else if (luaType.Contains("nil"))
        {
            description += "operation completes without returning a value";
        }
        else
        {
            description += $"result as {luaType}";
        }

        return description;
    }
}
