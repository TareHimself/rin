// See https://aka.ms/new-console-template for more information

using System.Text;
using Rin.Shading;
using Rin.Shading.Ast.Nodes;

//var tokens = Tokenizer.Run("<>",new MemoryStream(Encoding.UTF8.GetBytes("1.0e-10")));
var source = @"
// https://www.shadertoy.com/view/WtdSDs
struct BlurData
{
    float4x4 projection;
    float3x3 transform;
    float4 _options;
    float4 tint;
    fn getSize(): float2 {
        return _options.xy;
    }
    fn getStrength(): float {
        return _options.y;
    }
    fn getRadius(): float {
        return _options.w;
    }
}

push {
    BlurData *data;
}

fn rgb2hsv(float3 c): float3
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

scope compute {
    fn main(): void {
        
    }
}
";


var session = new Session();
var module = session.LoadSourceString("./test.rsh", source);
Dictionary<string, StructNode> structs = [];
module.Statements.Transverse(node =>
{
    if (node is StructNode asStructNode) structs.Add(asStructNode.Name, asStructNode);

    if (node is DeclarationNode asDeclarationNode)
        if (asDeclarationNode.Type is UnknownType asUnknownType)
            if (structs.TryGetValue(asUnknownType.TypeName, out var structFound))
                asDeclarationNode.Type = new StructTypeNode
                {
                    Struct = structFound
                };

    return true;
});

var output = new MemoryStream();
var transpiler = new SlangTranspiler();
if (module.TryGetScope("compute") is { } compute)
{
    var nodes = new List<INode>();
    nodes.AddRange(compute[..^1]);
    nodes.Add(new InjectNode
    {
        Code = "[shader(\"fragment\")]"
    });
    nodes.Add(compute.Last());
    transpiler.Transpile(nodes, output);
    var resultText = Encoding.UTF8.GetString(output.ToArray());

    Console.WriteLine("Hello, World!");
}