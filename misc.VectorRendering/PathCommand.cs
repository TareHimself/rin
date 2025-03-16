using System.Numerics;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using SixLabors.Fonts;
using TerraFX.Interop.Vulkan;
using Utils = Rin.Engine.Core.Utils;

namespace misc.VectorRendering;

public class PathCommand : CustomCommand
{
    private IShader _shader = SGraphicsModule.Get()
        .MakeGraphics(Path.Join(SViewsModule.ShadersDirectory, "path.slang"));

    private readonly Mat3 _transform;
    private readonly Font _font;
    public List<CurvePath> Paths = [];
    // private Bezier[] _beziers = [
    //     new Bezier
    //     {
    //         Begin = new Vector2(100, 0),
    //         End = new Vector2(200,100),
    //         Control = new Vector2(200, 0),
    //     },
    //     new Bezier
    //     {
    //         Begin = new Vector2(200,100),
    //         End = new Vector2(100,200),
    //         Control = new Vector2(200, 200),
    //     },
    //     new Bezier
    //     {
    //         Begin = new Vector2(100, 200),
    //         End = new Vector2(0,100),
    //         Control = new Vector2(100, 200) + (new Vector2(0,100) - new Vector2(100, 200)) / 2.0f//new Vector2(0, 200),
    //     },
    //     new Bezier
    //     {
    //         Begin = new Vector2(0,100),
    //         End = new Vector2(100, 0),
    //         Control = new Vector2(0, 0),
    //     }
    // ];

    public PathCommand(Mat3 transform,Font font,string text)
    {
        _transform = transform;
        _font = font;
        var renderer = new GlyphBezierRenderer();
        TextRenderer.RenderTextTo(renderer,text,new TextOptions(font));
        Paths.AddRange(renderer.GetPaths());
        //var z = paths.First();
    }
    
    struct PushConstant
    {
        public required Mat4 Projection;
        public required Mat3 Transform;
        public required Vector2 Size;
        public required int BezierCount;
        public required ulong BeziersAddress;
    }
    

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        var cmd = frame.Raw.GetCommandBuffer();

        if (_shader.Bind(cmd))
        {
            ulong offset = 0;
            foreach (var path in Paths)
            {
                
                var beziers = path.Curves;
                if(beziers.Length == 0) continue;
                var section = view?.GetView(offset,beziers.ByteSize());
                var min = new Vector2(float.PositiveInfinity);
                var max = new Vector2();
                foreach (var bezier in beziers)
                {
                    min = Vector2.Min(min, bezier.Begin);
                    min = Vector2.Min(min, bezier.End);
                    min = Vector2.Min(min, bezier.Control);

                    max = Vector2.Max(max, bezier.Begin);
                    max = Vector2.Max(max, bezier.End);
                    max = Vector2.Max(max, bezier.Control);
                }

                min = new Vector2(0.0f);
                section?.Write(beziers);
                
                cmd.PushConstant(_shader.GetPipelineLayout(),
                    VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT | VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
                    new PushConstant
                    {
                        Projection = frame.Projection,
                        Transform = _transform,
                        Size = max - min,
                        BezierCount = beziers.Length,
                        BeziersAddress = (section?.GetAddress() ?? 0),
                    });
                cmd.Draw(6);
                offset += beziers.ByteSize();
            }
        }
    }

    public override ulong GetRequiredMemory()
    {
        return Paths.Select(c => Utils.ByteSizeOf<Bezier>(c.Curves.Length)).Aggregate((a,b) => a + b);
    }

    public override bool WillDraw() => true;
}