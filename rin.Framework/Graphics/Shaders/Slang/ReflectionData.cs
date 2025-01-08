using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace rin.Framework.Graphics.Shaders.Slang;

public class ReflectionData
{
    
    public class ElementType
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;
        
        [JsonPropertyName("baseShape")]
        public string BaseShape { get; set; } = string.Empty;
    }
    
    public class ElementVarLayout
    {
        [JsonPropertyName("binding")]
        public ParameterBinding? Binding { get; set; }
        
        [JsonPropertyName("type")]
        public ParameterType Type  { get; set; } 
    }
    
    public class ParameterType
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;
        
        [JsonPropertyName("elementCount")]
        public int? ElementCount { get; set; }
        
        [JsonPropertyName("elementType")]
        public ElementType? ElementType { get; set; }
        
        [JsonPropertyName("baseShape")]
        public string? BaseShape { get; set; }
        
        [JsonPropertyName("elementVarLayout")]
        public ElementVarLayout? ElementVarLayout { get; set; }
    }
    
    public class ParameterBinding
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("arguments")] 
        public int? Binding { get; set; } = 0;
        
        [JsonPropertyName("space")] 
        public int? Set { get; set; } = 0;
        
        [JsonPropertyName("offset")] 
        public int? Offset { get; set; } = 0;
        
        [JsonPropertyName("size")] 
        public int? Size { get; set; } = 0;
    }
    
    public class UserAttributeField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")] 
        public JsonValue[] Arguments { get; set; } = [];
    }
    
    public class Parameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("userAttribs")] public UserAttributeField[] UserAttributes { get; set; } = [];
        
        [JsonPropertyName("binding")]
        public ParameterBinding? Binding { get; set; }
        
        [JsonPropertyName("bindings")]
        public ParameterBinding[]? Bindings { get; set; }
        
        [JsonPropertyName("type")]
        public ParameterType Type  { get; set; } 
    }

    public class EntryPoint
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// FIX THIS ONCE SLANG MAKES A NEW RELEASE https://github.com/shader-slang/slang/pull/5927
        /// </summary>
        [JsonPropertyName("stage:")]
        public string Stage { get; set; } = string.Empty;
        
        [JsonPropertyName("parameters")]
        public Parameter[] Parameters { get; set; } = [];
    }
    
    [JsonPropertyName("parameters")]
    public Parameter[] Parameters { get; set; } = [];
    
    [JsonPropertyName("entryPoints")]
    public EntryPoint[] EntryPoints { get; set; } = [];
}