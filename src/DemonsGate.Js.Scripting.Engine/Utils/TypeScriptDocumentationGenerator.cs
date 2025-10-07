using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using DemonsGate.Core.Attributes.Scripts;
using DemonsGate.Core.Data.Scripts;
using DemonsGate.Core.Extensions.Strings;

namespace DemonsGate.Js.Scripting.Engine.Utils;

/// <summary>
///     Utility class for generating TypeScript definition files from C# script modules
///     Automatically creates .d.ts files with function signatures, types, and documentation
/// </summary>
[RequiresUnreferencedCode(
    "This class uses reflection to analyze types for TypeScript generation and requires full type metadata."
)]
public static class TypeScriptDocumentationGenerator
{
    private static readonly HashSet<Type> _processedTypes = new();
    private static readonly StringBuilder _interfacesBuilder = new();
    private static readonly StringBuilder _constantsBuilder = new();
    private static readonly StringBuilder _enumsBuilder = new();
    private static readonly List<Type> _interfaceTypesToGenerate = new(32);
    private static readonly Dictionary<Type, bool> _recordTypeCache = new();

    private static Func<string, string> _nameResolver = name => name.ToSnakeCase();

    public static List<Type> FoundEnums { get; } = new(16);

    public static void AddInterfaceToGenerate(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _interfaceTypesToGenerate.Add(type);
    }

    /// <summary>
    ///     Clears all internal caches and state. Useful for testing or when generating multiple separate documentation files.
    /// </summary>
    public static void ClearCaches()
    {
        _processedTypes.Clear();
        _recordTypeCache.Clear();
        _interfaceTypesToGenerate.Clear();
        FoundEnums.Clear();
        _interfacesBuilder.Clear();
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
        sb.AppendLine("/**");
        sb.AppendLine(CultureInfo.InvariantCulture, $" * {appName} v{appVersion} JavaScript API TypeScript Definitions");
        sb.AppendLine(
            CultureInfo.InvariantCulture,
            $" * Auto-generated documentation on {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        );
        sb.AppendLine(" **/");
        sb.AppendLine();

        // Start global declaration block
        sb.AppendLine("// Global declarations for Spectra Engine API");
        sb.AppendLine("declare global {");

        // Reset processed types and builders for this generation run
        _processedTypes.Clear();
        _interfacesBuilder.Clear();
        _constantsBuilder.Clear();
        _enumsBuilder.Clear();
        _interfaceTypesToGenerate.Clear();

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

            sb.AppendLine("    /**");
            sb.AppendLine(CultureInfo.InvariantCulture, $"     * {module.ModuleType.Name} module");

            if (!string.IsNullOrWhiteSpace(moduleHelpText))
            {
                sb.AppendLine("     *");
                sb.AppendLine(CultureInfo.InvariantCulture, $"     * {moduleHelpText}");
            }

            sb.AppendLine("     */");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    const {moduleName}: {{");

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

                var functionName = _nameResolver(method.Name);
                var description = scriptFunctionAttr.HelpText ?? "No description available";

                // Generate function documentation
                sb.AppendLine("        /**");
                sb.AppendLine(CultureInfo.InvariantCulture, $"         * {description}");

                // Add remarks section if available
                var remarksText = GetRemarksText(method, scriptFunctionAttr);
                if (!string.IsNullOrEmpty(remarksText))
                {
                    sb.AppendLine("         *");
                    sb.AppendLine("         * @remarks");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"         * {remarksText}");
                }

                // Add parameter documentation with enhanced format
                var parameters = method.GetParameters();
                if (parameters.Length > 0)
                {
                    sb.AppendLine("         *");
                    foreach (var param in parameters)
                    {
                        var paramType = ConvertToTypeScriptType(param.ParameterType);
                        var paramName = param.Name ?? $"param{Array.IndexOf(parameters, param)}";
                        var paramDescription = GetParameterDescription(param, paramType);
                        sb.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"         * @param {_nameResolver(paramName)} - {paramDescription}"
                        );
                    }
                }

                // Add return type documentation if not void with enhanced format
                if (method.ReturnType != typeof(void))
                {
                    var returnType = ConvertToTypeScriptType(method.ReturnType);
                    var returnDescription = GetReturnDescription(method.ReturnType, returnType);
                    sb.AppendLine("         *");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"         * @returns {returnDescription}");
                }

                // Add additional JSDoc tags if available
                var additionalTags = GetAdditionalJSDocTags(method, scriptFunctionAttr);
                if (!string.IsNullOrEmpty(additionalTags))
                {
                    sb.AppendLine("         *");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"         * {additionalTags}");
                }

                sb.AppendLine("         */");

                // Generate function signature
                sb.Append(CultureInfo.InvariantCulture, $"        {functionName}(");

                // Generate parameters
                for (var i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramType = ConvertToTypeScriptType(param.ParameterType);
                    var paramName = param.Name ?? $"param{i}";
                    var isOptional = param.IsOptional || param.ParameterType.IsByRef ||
                                     param.ParameterType.IsGenericType &&
                                     param.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>) ||
                                     paramType.EndsWith("[]?", StringComparison.Ordinal);

                    sb.Append(
                        CultureInfo.InvariantCulture,
                        $"{_nameResolver(paramName)}{(isOptional ? "?" : "")}: {paramType}"
                    );

                    if (i < parameters.Length - 1)
                    {
                        sb.Append(", ");
                    }
                }

                // Add return type
                var methodReturnType = ConvertToTypeScriptType(method.ReturnType);
                sb.AppendLine(CultureInfo.InvariantCulture, $"): {methodReturnType};");
            }

            sb.AppendLine("    };");
            sb.AppendLine();
        }

        // Generate all interfaces that were collected during type conversion
        GenerateAllInterfaces();

        // Append enums and interfaces more efficiently
        sb.Append(_enumsBuilder);
        sb.AppendLine();
        sb.Append(_interfacesBuilder);

        // Close global declaration block
        sb.AppendLine("}");
        sb.AppendLine();

        // Export types for module usage
        sb.AppendLine("// Export types for module usage");
        foreach (var enumType in FoundEnums)
        {
            sb.AppendLine($"export {{ {_nameResolver(enumType.Name)} }};");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Method to generate all interfaces after collecting them
    /// </summary>
    private static void GenerateAllInterfaces()
    {
        // Use a more efficient approach to handle dependencies between types
        var processedInIteration = new HashSet<Type>();
        var remainingTypes = new List<Type>(_interfaceTypesToGenerate);

        while (remainingTypes.Count > 0)
        {
            processedInIteration.Clear();

            // Process types that can be processed in this iteration
            for (var i = remainingTypes.Count - 1; i >= 0; i--)
            {
                var type = remainingTypes[i];

                // Skip if already processed
                if (_processedTypes.Contains(type))
                {
                    remainingTypes.RemoveAt(i);
                    continue;
                }

                // Check if all dependencies are processed
                if (CanProcessType(type))
                {
                    GenerateInterface(type);
                    processedInIteration.Add(type);
                    remainingTypes.RemoveAt(i);
                }
            }

            // If no progress was made, process remaining types anyway to avoid infinite loop
            if (processedInIteration.Count == 0 && remainingTypes.Count > 0)
            {
                foreach (var type in remainingTypes)
                {
                    if (!_processedTypes.Contains(type))
                    {
                        GenerateInterface(type);
                    }
                }

                break;
            }
        }
    }

    private static bool CanProcessType(Type type)
    {
        // For now, assume all types can be processed
        // This could be enhanced to check for actual dependencies
        return true;
    }

    /// <summary>
    ///     Check if a type is a C# record type (with caching for performance)
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

        // Check cache first
        if (_recordTypeCache.TryGetValue(type, out var isRecord))
        {
            return isRecord;
        }

        // C# records have specific characteristics:
        // 1. They are classes
        // 2. They have a compiler-generated EqualityContract property
        // 3. They have specific compiler-generated methods

        if (!type.IsClass)
        {
            _recordTypeCache[type] = false;
            return false;
        }

        // Check for the EqualityContract property which is generated for all record types
        var equalityContract = type.GetProperty(
            "EqualityContract",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (equalityContract is not null && equalityContract.PropertyType == typeof(Type))
        {
            _recordTypeCache[type] = true;
            return true;
        }

        // Alternative check: look for compiler-generated attributes or methods
        // Records have compiler-generated ToString, GetHashCode, Equals methods
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
    ///     Method to generate a single interface
    /// </summary>
    [SuppressMessage("Trimming", "IL2070:Reflection", Justification = "Reflection is required for interface generation")]
    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for interface generation")]
    private static void GenerateInterface(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors
        )]
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!_processedTypes.Add(type))
        {
            return; // Already processed
        }

        var interfaceName = $"I{type.Name}";

        // Start building the interface
        _interfacesBuilder.AppendLine();
        _interfacesBuilder.AppendLine("/**");

        if (IsRecordType(type))
        {
            _interfacesBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $" * Generated interface for record type {type.FullName}"
            );
        }
        else
        {
            _interfacesBuilder.AppendLine(CultureInfo.InvariantCulture, $" * Generated interface for {type.FullName}");
        }

        _interfacesBuilder.AppendLine(" */");
        _interfacesBuilder.AppendLine(CultureInfo.InvariantCulture, $"interface {interfaceName} {{");

        // Get properties - for records, we want to include both public properties and constructor parameters
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        // For record types, also check constructor parameters to ensure we get all record properties
        if (IsRecordType(type))
        {
            // Get the primary constructor (the one with the most parameters, typically the record constructor)
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (primaryConstructor is not null)
            {
                var constructorParams = primaryConstructor.GetParameters();
                foreach (var param in constructorParams)
                {
                    var paramName = param.Name;
                    if (string.IsNullOrEmpty(paramName))
                    {
                        continue;
                    }

                    // Check if we already have a property with this name
                    var existingProperty = properties.FirstOrDefault(p =>
                        string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase)
                    );

                    if (existingProperty is null)
                    {
                        // Add this parameter as a property in our interface
                        var paramType = ConvertToTypeScriptType(param.ParameterType);

                        _interfacesBuilder.AppendLine("    /**");
                        _interfacesBuilder.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"     * {_nameResolver(paramName)} (from record constructor)"
                        );
                        _interfacesBuilder.AppendLine("     */");
                        _interfacesBuilder.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"    {_nameResolver(paramName)}: {paramType};"
                        );
                    }
                }
            }
        }

        foreach (var property in properties)
        {
            var propertyName = property.Name;
            if (string.IsNullOrEmpty(propertyName))
            {
                continue;
            }

            var propertyType = ConvertToTypeScriptType(property.PropertyType);

            // Add property documentation
            _interfacesBuilder.AppendLine("    /**");
            _interfacesBuilder.AppendLine(CultureInfo.InvariantCulture, $"     * {_nameResolver(propertyName)}");
            _interfacesBuilder.AppendLine("     */");

            // Add property
            _interfacesBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $"    {_nameResolver(propertyName)}: {propertyType};"
            );
        }

        // End interface - make sure it's properly closed
        _interfacesBuilder.AppendLine("}");
    }

    [SuppressMessage("Trimming", "IL2070:Reflection", Justification = "Reflection is required for TypeScript generation")]
    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for TypeScript generation")]
    [SuppressMessage("Trimming", "IL2062:Reflection", Justification = "Reflection is required for TypeScript generation")]
    private static string ConvertToTypeScriptType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicConstructors
        )]
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);

        // Handle primitive types
        if (type == typeof(void))
        {
            return "void";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(float) ||
            type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
            type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
            type == typeof(byte) || type == typeof(sbyte))
        {
            return "number";
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type == typeof(object))
        {
            return "any";
        }

        if (type == typeof(object[]))
        {
            return "any[]";
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType is null)
            {
                return "any[]";
            }

            return $"{ConvertToTypeScriptType(elementType)}[]";
        }

        // Handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is null)
            {
                return "any";
            }

            return $"{ConvertToTypeScriptType(underlyingType)} | null";
        }

        // Handle params object[]? case
        if (type.IsArray && type.GetElementType() == typeof(object) && type.Name.EndsWith("[]", StringComparison.Ordinal))
        {
            return "any[]?";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            // Handle Dictionary<TKey, TValue>
            if (genericTypeDefinition == typeof(Dictionary<,>))
            {
                var keyType = ConvertToTypeScriptType(genericArgs[0]);
                var valueType = ConvertToTypeScriptType(genericArgs[1]);

                // For string keys, use standard record type
                if (genericArgs[0] == typeof(string))
                {
                    return $"{{ [key: string]: {valueType} }}";
                }

                // For other keys, use Map
                return $"Map<{keyType}, {valueType}>";
            }

            // Handle Record<TKey, TValue>
            if (type.Name.StartsWith("Record`", StringComparison.Ordinal) && genericArgs.Length == 2)
            {
                var keyType = ConvertToTypeScriptType(genericArgs[0]);
                var valueType = ConvertToTypeScriptType(genericArgs[1]);

                // For string keys, use TypeScript Record type
                if (genericArgs[0] == typeof(string))
                {
                    return $"Record<string, {valueType}>";
                }

                // For other key types, use TypeScript Record type
                return $"Record<{keyType}, {valueType}>";
            }

            // Handle Action delegates
            if (genericTypeDefinition == typeof(Action))
            {
                return "() => void";
            }

            if (genericTypeDefinition == typeof(Action<>))
            {
                var paramName = GetDescriptiveParameterName(genericArgs[0]);
                return $"({paramName}: {ConvertToTypeScriptType(genericArgs[0])}) => void";
            }

            if (genericTypeDefinition == typeof(Action<,>))
            {
                var param1Name = GetDescriptiveParameterName(genericArgs[0]);
                var param2Name = GetDescriptiveParameterName(genericArgs[1]);
                return
                    $"({param1Name}: {ConvertToTypeScriptType(genericArgs[0])}, {param2Name}: {ConvertToTypeScriptType(genericArgs[1])}) => void";
            }

            if (genericTypeDefinition == typeof(Action<,,>))
            {
                var param1Name = GetDescriptiveParameterName(genericArgs[0]);
                var param2Name = GetDescriptiveParameterName(genericArgs[1]);
                var param3Name = GetDescriptiveParameterName(genericArgs[2]);
                return
                    $"({param1Name}: {ConvertToTypeScriptType(genericArgs[0])}, {param2Name}: {ConvertToTypeScriptType(genericArgs[1])}, {param3Name}: {ConvertToTypeScriptType(genericArgs[2])}) => void";
            }

            if (genericTypeDefinition == typeof(Action<,,,>))
            {
                var param1Name = GetDescriptiveParameterName(genericArgs[0]);
                var param2Name = GetDescriptiveParameterName(genericArgs[1]);
                var param3Name = GetDescriptiveParameterName(genericArgs[2]);
                var param4Name = GetDescriptiveParameterName(genericArgs[3]);
                return
                    $"({param1Name}: {ConvertToTypeScriptType(genericArgs[0])}, {param2Name}: {ConvertToTypeScriptType(genericArgs[1])}, {param3Name}: {ConvertToTypeScriptType(genericArgs[2])}, {param4Name}: {ConvertToTypeScriptType(genericArgs[3])}) => void";
            }

            // Handle Func delegates
            if (genericTypeDefinition == typeof(Func<>))
            {
                return $"() => {ConvertToTypeScriptType(genericArgs[0])}";
            }

            if (genericTypeDefinition == typeof(Func<,>))
            {
                var paramName = GetDescriptiveParameterName(genericArgs[0]);
                return $"({paramName}: {ConvertToTypeScriptType(genericArgs[0])}) => {ConvertToTypeScriptType(genericArgs[1])}";
            }

            if (genericTypeDefinition == typeof(Func<,,>))
            {
                var param1Name = GetDescriptiveParameterName(genericArgs[0]);
                var param2Name = GetDescriptiveParameterName(genericArgs[1]);
                return
                    $"({param1Name}: {ConvertToTypeScriptType(genericArgs[0])}, {param2Name}: {ConvertToTypeScriptType(genericArgs[1])}) => {ConvertToTypeScriptType(genericArgs[2])}";
            }

            // Handle List<T>
            if (genericTypeDefinition == typeof(List<>))
            {
                return $"{ConvertToTypeScriptType(genericArgs[0])}[]";
            }

            // Handle other collections
            if (genericTypeDefinition == typeof(IEnumerable<>) || genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IReadOnlyCollection<>) || genericTypeDefinition == typeof(IList<>))
            {
                return $"{ConvertToTypeScriptType(genericArgs[0])}[]";
            }
        }

        // Handle C# record types explicitly
        if (IsRecordType(type))
        {
            // Generate interface name for record
            var interfaceName = $"I{type.Name}";

            // If we've already processed this type, just return the interface name
            if (_processedTypes.Contains(type))
            {
                return interfaceName;
            }

            // Add this type to our list of types that need interfaces generated
            if (!_interfaceTypesToGenerate.Contains(type))
            {
                _interfaceTypesToGenerate.Add(type);
            }

            return interfaceName;
        }

        // For complex types (classes and structs), generate interfaces
        if ((type.IsClass || type.IsValueType) && !type.IsPrimitive && !type.IsEnum &&
            type.Namespace is not null && !type.Namespace.StartsWith("System", StringComparison.Ordinal))
        {
            // Generate interface name
            var interfaceName = $"I{type.Name}";

            // If we've already processed this type, just return the interface name
            if (_processedTypes.Contains(type))
            {
                return interfaceName;
            }

            // Add this type to our list of types that need interfaces generated
            if (!_interfaceTypesToGenerate.Contains(type))
            {
                _interfaceTypesToGenerate.Add(type);
            }

            return interfaceName;
        }

        // Handle enums
        if (type.IsEnum)
        {
            GenerateEnumInterface(type);
            return _nameResolver(type.Name);
        }

        // Handle other delegate types
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var method = type.GetMethod("Invoke");
            if (method is not null)
            {
                var parameters = method.GetParameters();
                var paramStrings = parameters.Select((p, i) =>
                    {
                        var paramName = p.Name ?? GetDescriptiveParameterName(p.ParameterType);
                        return $"{paramName}: {ConvertToTypeScriptType(p.ParameterType)}";
                    }
                );
                var returnType = ConvertToTypeScriptType(method.ReturnType);
                return $"({string.Join(", ", paramStrings)}) => {returnType}";
            }

            return "(...args: any[]) => any";
        }

        // For other complex types, return any
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
            return "null";
        }

        if (type == typeof(string))
        {
            return $"\"{value}\"";
        }

        if (type == typeof(bool))
        {
            return value.ToString().ToLowerInvariant();
        }

        if (type.IsEnum)
        {
            return $"{_nameResolver(type.Name)}.{value}";
        }

        // For numerical values and other types
        return value.ToString() ?? "null";
    }

    [SuppressMessage("Trimming", "IL2072:Reflection", Justification = "Reflection is required for constant type analysis")]
    private static void ProcessConstants(Dictionary<string, object> constants)
    {
        ArgumentNullException.ThrowIfNull(constants);

        if (constants.Count == 0)
        {
            return;
        }

        foreach (var constant in constants)
        {
            var constantName = constant.Key ?? "unnamed";
            var constantValue = constant.Value;
            var constantType = constantValue?.GetType() ?? typeof(object);

            var typeScriptType = ConvertToTypeScriptType(constantType);
            var formattedValue = FormatConstantValue(constantValue, constantType);

            // Generate constant documentation
            _constantsBuilder.AppendLine("    /**");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"     * {constantName} constant");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"     * Value: {formattedValue}");
            _constantsBuilder.AppendLine("     */");
            _constantsBuilder.AppendLine(CultureInfo.InvariantCulture, $"    const {constantName}: {typeScriptType};");
            _constantsBuilder.AppendLine();
        }

        _constantsBuilder.AppendLine();
    }

    [SuppressMessage(
        "Trimming",
        "IL2070:Reflection",
        Justification = "Reflection is required for enum interface generation"
    )]
    private static void GenerateEnumInterface(Type enumType)
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
        _enumsBuilder.AppendLine("    /**");
        _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"     * Generated enum for {enumType.FullName}");
        _enumsBuilder.AppendLine("     */");
        _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"    enum {_nameResolver(enumType.Name)} {{");

        var enumValues = Enum.GetNames(enumType);
        var enumUnderlyingType = Enum.GetUnderlyingType(enumType);

        foreach (var value in enumValues)
        {
            try
            {
                var enumValue = Enum.Parse(enumType, value);
                var numericValue = Convert.ChangeType(enumValue, enumUnderlyingType);
                _enumsBuilder.AppendLine(CultureInfo.InvariantCulture, $"        {value} = {numericValue},");
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException)
            {
                // Handle the case where the enum value cannot be converted
                // This can happen if the enum is defined with a different underlying type
                _enumsBuilder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"        {value} = 0, // Unable to determine numeric value"
                );
            }
        }

        _enumsBuilder.AppendLine("    }");
    }

    /// <summary>
    ///     Gets remarks text for a method based on its attributes and module context
    /// </summary>
    private static string GetRemarksText(MethodInfo method, ScriptFunctionAttribute scriptFunctionAttr)
    {
        // Check for custom remarks in attributes or generate context-aware remarks
        var moduleName = method.DeclaringType?.GetCustomAttribute<ScriptModuleAttribute>()?.Name;

        if (!string.IsNullOrEmpty(moduleName))
        {
            return $"This method is part of the {{@link {moduleName.ToLowerInvariant()}-module | {moduleName} module}}.";
        }

        return string.Empty;
    }

    /// <summary>
    ///     Gets a descriptive parameter name based on the type
    /// </summary>
    private static string GetDescriptiveParameterName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Get the base type name without generic parameters
        var typeName = type.Name;

        // Remove generic type indicators (e.g., `1, `2)
        var genericTickIndex = typeName.IndexOf('`');
        if (genericTickIndex >= 0)
        {
            typeName = typeName[..genericTickIndex];
        }

        // Remove common prefixes
        if (typeName.StartsWith("Script", StringComparison.Ordinal) && typeName.Length > 6)
        {
            typeName = typeName[6..]; // Remove "Script" prefix
        }

        // Convert to camelCase
        if (typeName.Length > 0)
        {
            typeName = char.ToLowerInvariant(typeName[0]) + typeName[1..];
        }

        // Handle common naming patterns to make them more concise
        typeName = typeName switch
        {
            "executionContext" => "context",
            "commandRequest"   => "request",
            "commandResult"    => "result",
            "eventArgs"        => "args",
            "eventData"        => "data",
            "configuration"    => "config",
            "information"      => "info",
            "parameters"       => "params",
            _                  => typeName
        };

        // Ensure the name is not empty or too short
        if (string.IsNullOrEmpty(typeName) || typeName.Length < 2)
        {
            return "arg";
        }

        return typeName;
    }

    /// <summary>
    ///     Gets enhanced parameter description with type information
    /// </summary>
    private static string GetParameterDescription(ParameterInfo param, string typeScriptType)
    {
        var baseName = param.Name ?? "parameter";
        var description = $"The {baseName.ToLowerInvariant()}";

        // Add type-specific context
        if (typeScriptType.Contains("number"))
        {
            description += " value";
        }
        else if (typeScriptType.Contains("string"))
        {
            description += " text";
        }
        else if (typeScriptType.Contains("boolean"))
        {
            description += " flag";
        }
        else if (typeScriptType.Contains("[]"))
        {
            description += " array";
        }
        else if (typeScriptType.Contains("object") || typeScriptType.Contains("interface"))
        {
            description += " object";
        }
        else
        {
            description += $" of type `{typeScriptType}`";
        }

        // Add optional indicator
        if (param.IsOptional)
        {
            description += " (optional)";
        }

        return description;
    }

    /// <summary>
    ///     Gets enhanced return description with type information
    /// </summary>
    private static string GetReturnDescription(Type returnType, string typeScriptType)
    {
        var description = "The ";

        // Add type-specific context
        if (typeScriptType.Contains("number"))
        {
            description += "computed numeric value";
        }
        else if (typeScriptType.Contains("string"))
        {
            description += "resulting text";
        }
        else if (typeScriptType.Contains("boolean"))
        {
            description += "result of the operation";
        }
        else if (typeScriptType.Contains("[]"))
        {
            description += "collection of results";
        }
        else if (typeScriptType.Contains("Promise"))
        {
            description += "promise that resolves to the operation result";
        }
        else if (typeScriptType.Contains("void"))
        {
            description += "operation completes without returning a value";
        }
        else
        {
            description += $"result as `{typeScriptType}`";
        }

        return description;
    }

    /// <summary>
    ///     Gets additional JSDoc tags based on method characteristics
    /// </summary>
    private static string GetAdditionalJSDocTags(MethodInfo method, ScriptFunctionAttribute scriptFunctionAttr)
    {
        var tags = new List<string>();

        // Add @beta tag for new or experimental methods
        if (IsExperimentalMethod(method))
        {
            tags.Add("@beta");
        }

        // Add @deprecated tag if method is marked as obsolete
        if (method.GetCustomAttribute<ObsoleteAttribute>() != null)
        {
            tags.Add("@deprecated");
        }

        // Add @since tag based on versioning if available
        var sinceVersion = GetMethodSinceVersion(method);
        if (!string.IsNullOrEmpty(sinceVersion))
        {
            tags.Add($"@since {sinceVersion}");
        }

        // Add @example tag if we have usage examples
        var exampleText = GetMethodExample(method);
        if (!string.IsNullOrEmpty(exampleText))
        {
            tags.Add("@example");
            tags.Add("```typescript");
            tags.Add(exampleText);
            tags.Add("```");
        }

        return string.Join("\n     * ", tags);
    }

    /// <summary>
    ///     Determines if a method is experimental based on naming or attributes
    /// </summary>
    private static bool IsExperimentalMethod(MethodInfo method)
    {
        // Check method name patterns
        var methodName = method.Name.ToLowerInvariant();
        if (methodName.Contains("experimental") || methodName.Contains("beta") || methodName.Contains("preview"))
        {
            return true;
        }

        // Check for experimental attributes (you could add custom attributes)
        return false;
    }

    /// <summary>
    ///     Gets the version when a method was introduced
    /// </summary>
    private static string GetMethodSinceVersion(MethodInfo method)
    {
        // This could be enhanced to read from custom attributes or metadata
        // For now, return empty string
        return string.Empty;
    }

    /// <summary>
    ///     Generates a simple usage example for the method
    /// </summary>
    private static string GetMethodExample(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var moduleName = method.DeclaringType?.GetCustomAttribute<ScriptModuleAttribute>()?.Name?.ToLowerInvariant();
        var methodName = _nameResolver(method.Name);

        if (string.IsNullOrEmpty(moduleName))
        {
            return string.Empty;
        }

        var exampleParams = new List<string>();
        foreach (var param in parameters)
        {
            var paramName = param.Name ?? "param";
            var typeScriptType = ConvertToTypeScriptType(param.ParameterType);

            if (typeScriptType.Contains("string"))
            {
                exampleParams.Add($"\"{paramName}\"");
            }
            else if (typeScriptType.Contains("number"))
            {
                exampleParams.Add("42");
            }
            else if (typeScriptType.Contains("boolean"))
            {
                exampleParams.Add("true");
            }
            else
            {
                exampleParams.Add($"{paramName}");
            }
        }

        var paramString = string.Join(", ", exampleParams);

        if (method.ReturnType == typeof(void))
        {
            return $"{moduleName}.{methodName}({paramString});";
        }

        return $"const result = {moduleName}.{methodName}({paramString});";
    }
}
