using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Rin.Slang;

public class SlangReflectionData
{
    [JsonPropertyName("parameters")] public Parameter[] Parameters { get; set; } = [];

    [JsonPropertyName("entryPoints")] public EntryPoint[] EntryPoints { get; set; } = [];

    public class ElementVarLayout
    {
        [JsonPropertyName("binding")] public ParameterBinding? Binding { get; set; }

        [JsonPropertyName("type")] public ParameterType Type { get; set; }
    }

    /// <summary>
    ///     Recursive - covers a plain resource/scalar/array shape, a user struct's own shape (with
    ///     <see cref="Fields" />), and a <c>ParameterBlock&lt;T&gt;</c> (kind "parameterBlock", whose
    ///     own <see cref="ElementType" /> is that struct shape). One type, not a separate simpler type
    ///     for "leaf" cases - a ParameterBlock's element can itself contain arbitrarily nested structs.
    /// </summary>
    public class ParameterType
    {
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("elementCount")] public int? ElementCount { get; set; }

        [JsonPropertyName("elementType")] public ParameterType? ElementType { get; set; }

        [JsonPropertyName("baseShape")] public string? BaseShape { get; set; }

        [JsonPropertyName("elementVarLayout")] public ElementVarLayout? ElementVarLayout { get; set; }

        /// <summary>A struct's (or a ParameterBlock's element struct's) own members, each with its own binding.</summary>
        [JsonPropertyName("fields")] public Field[]? Fields { get; set; }
    }

    public class ParameterBinding
    {
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;

        // "descriptorTableSlot": the binding index within its set. "subElementRegisterSpace" (a
        // ParameterBlock's own binding): despite the name, this is the actual set/space index itself -
        // confirmed empirically against real Slang -reflection-json output, there is no separate
        // "space" key alongside it for that binding kind.
        [JsonPropertyName("index")] public int? Binding { get; set; } = 0;

        [JsonPropertyName("space")] public int? Set { get; set; } = 0;

        [JsonPropertyName("offset")] public int? Offset { get; set; } = 0;

        [JsonPropertyName("size")] public int? Size { get; set; } = 0;
    }

    public class UserAttributeField
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")] public JsonValue[] Arguments { get; set; } = [];
    }

    /// <summary>One member of a struct-shaped ParameterType.Fields - a resource, a nested struct, or a plain value.</summary>
    public class Field
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")] public ParameterType Type { get; set; }

        [JsonPropertyName("binding")] public ParameterBinding? Binding { get; set; }

        [JsonPropertyName("userAttribs")] public UserAttributeField[] UserAttributes { get; set; } = [];
    }

    public class Parameter
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("userAttribs")] public UserAttributeField[] UserAttributes { get; set; } = [];

        [JsonPropertyName("binding")] public ParameterBinding? Binding { get; set; }

        [JsonPropertyName("bindings")] public ParameterBinding[]? Bindings { get; set; }

        [JsonPropertyName("type")] public ParameterType Type { get; set; }
    }

    public class EntryPointResultTypeField
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("semanticName")] public string SemanticName { get; set; } = string.Empty;

        [JsonPropertyName("userAttribs")] public UserAttributeField[] UserAttributes { get; set; } = [];
    }

    public class EntryPointResultType
    {
        [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("fields")] public EntryPointResultTypeField[] Fields { get; set; } = [];
    }

    public class EntryPointResult
    {
        [JsonPropertyName("type")] public EntryPointResultType Type { get; set; }
    }

    public class EntryPoint
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     FIX THIS ONCE SLANG MAKES A NEW RELEASE https://github.com/shader-slang/slang/pull/5927
        /// </summary>
        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("parameters")] public Parameter[] Parameters { get; set; } = [];

        [JsonPropertyName("threadGroupSize")] public uint[] ThreadGroupSize { get; set; } = [];

        [JsonPropertyName("result")] public EntryPointResult? Result { get; set; }

        [JsonPropertyName("userAttribs")] public UserAttributeField[] UserAttributes { get; set; } = [];
    }
}
