using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.PassConfigs;

namespace Rin.Engine.Views.Graphics.CommandHandlers;

public class BlurCommandHandler : ICommandHandler
{
    internal struct BlurData()
    {
        public required ImageHandle SourceT;

        public required Matrix4x4 Projection = Matrix4x4.Identity;

        public required Matrix4x4 Transform = Matrix4x4.Identity;

        private Vector4 _options = Vector4.Zero;

        public Vector2 Size
        {
            get => new(_options.X, _options.Y);
            set
            {
                _options.X = value.X;
                _options.Y = value.Y;
            }
        }

        public float Strength
        {
            get => _options.Z;
            set => _options.Z = value;
        }

        public float Radius
        {
            get => _options.W;
            set => _options.W = value;
        }

        public Vector4 Tint = Vector4.Zero;
    }
    
    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/blur.slang");
    private uint _bufferId;

    private BlurCommand[] _commands = [];

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<BlurCommand>().ToArray();
    }

    public void Configure(IGraphConfig config, IPassConfig passConfig)
    {
        
        _bufferId = config.CreateBuffer<BlurData>(_commands.Length, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx, IPassConfig passConfig)
    {
        Debug.Assert(passConfig is BlurPassConfig);
        
        if (_blurShader.Bind(ctx))
        {
            var blurPassConfig = (BlurPassConfig)passConfig;
            
            var copyImage = graph.GetImage(blurPassConfig.CopyImageId);
            var buffer = graph.GetBufferOrException(_bufferId);

            foreach (var blur in _commands)
            {
                ctx.SetStencilCompareMask(blur.StencilMask);
                buffer.WriteStruct(new BlurData
                {
                    SourceT = copyImage.BindlessHandle,
                    Projection = blurPassConfig.PassContext.ProjectionMatrix,
                    Size = blur.Size,
                    Strength = blur.Strength,
                    Radius = blur.Radius,
                    Tint = blur.Tint,
                    Transform = blur.Transform
                });
                _blurShader.Push(ctx, buffer.GetAddress());
                ctx
                    .Draw(6);
            }

            ctx.EndRendering();
        }
    }
}