using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.PassConfigs;

namespace Rin.Framework.Views.Graphics.Blur;

internal class BlurInitCommandHandler : ICommandHandler
{
    private const float _scaleFactor = 7f;
    private BlurInitCommand[] _commands = [];
    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<BlurInitCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        foreach (var command in _commands)
        {

            var blurArea = command.BoundingBoxP2 - command.BoundingBoxP1;
            var reductionFactor = float.Min(1,_scaleFactor / command.Radius.X);
            var newSize = blurArea * reductionFactor;
            newSize = newSize.Clamp(blurArea * 0.05f, new Vector2(surfaceContext.Extent.Width, surfaceContext.Extent.Height));
            
            //newSize = blurArea;
            
            command.BlurP1 = Vector2.Zero;
            command.BlurP2 = newSize = newSize.Ceiling();
            
            command.BlurRadius = (newSize / blurArea) * command.Radius;
            var min = float.Min(command.BlurRadius.X, command.BlurRadius.Y);
            command.BlurRadius = new Vector2(min);
            var extent = new Extent2D((uint)float.Ceiling(newSize.X),(uint)float.Ceiling(newSize.Y));
            command.FirstPassImageId = config.CreateTexture(extent, ImageFormat.RGBA8, ImageLayout.TransferDst);
            command.SecondPassImageId = config.CreateTexture(extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
            command.LocalProjection = MathR.ViewportProjection(newSize.X, newSize.Y, 0, 1f);
        }
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        Debug.Assert(passConfig is BlurInitPassConfig);

        var srcImage = graph.GetTexture(surfaceContext.MainImageId);
        foreach (var command in _commands)
        {
            var srcOffset = new Offset2D(
                (int)command.BoundingBoxP1.X,
                (int)command.BoundingBoxP1.Y);
            var srcSize = command.BoundingBoxP2 - command.BoundingBoxP1;
            
            var destImage = graph.GetTexture(command.FirstPassImageId);
            ctx.CopyToImage(srcImage,
                srcOffset,new Extent2D(
                    (int)srcSize.X,
                    (int)srcSize.Y), destImage, new Offset2D(),destImage.Extent);
        }
    }
}

internal struct BlurData()
{
        
    public required Matrix4x4 Transform = Matrix4x4.Identity;

    public required Matrix4x4 Projection = Matrix4x4.Identity;
    public required ImageHandle SourceT;
    public required Vector2 Size;
    public required float Strength;
    public required Vector2 Radius;
    public Vector4 Tint = Vector4.Zero;
    public required Vector4 DestRect;
}

internal struct Push
{
    public required ulong BufferAddress;
    public required int IsHorizontal;
}

internal class BlurFirstPassCommandHandler : ICommandHandler
{
    private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("Framework/Shaders/Views/blur.slang");
    private BlurFirstPassCommand[] _commands = [];
    
    private uint[] _bufferIds = [];
    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<BlurFirstPassCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        foreach (var command in _commands)
        {
            config.ReadTexture(command.InitCommand.FirstPassImageId, ImageLayout.ShaderReadOnly);
            config.WriteTexture(command.InitCommand.SecondPassImageId, ImageLayout.ColorAttachment);
        }
        _bufferIds = Enumerable.Range(0, _commands.Length)
            .Select(_ => config.CreateBuffer<BlurData>(GraphBufferUsage.HostThenGraphics)).ToArray();
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        Debug.Assert(passConfig is BlurPassConfig);
        
        foreach (var (command,bufferId) in _commands.Zip(_bufferIds))
        {
            var srcImage = graph.GetTexture(command.InitCommand.FirstPassImageId);
            var dstImage = graph.GetTexture(command.InitCommand.SecondPassImageId);
            var buffer = graph.GetBufferOrException(bufferId);
            ctx.BeginRendering(dstImage.Extent, [dstImage],clearColor: Vector4.Zero);
            if (_shader.Bind(ctx) is { } bindContext)
            {
                buffer.Write(new BlurData
                {
                    SourceT = srcImage.Handle,
                    Projection = command.InitCommand.LocalProjection,
                    Size = new Vector2(srcImage.Extent.Width,srcImage.Extent.Height),
                    Strength = command.InitCommand.Strength,
                    Radius = command.InitCommand.BlurRadius,
                    Tint = command.InitCommand.Tint,
                    Transform = command.InitCommand.LocalTransform,
                    DestRect = new Vector4(command.InitCommand.BlurP1,command.InitCommand.BlurP2.X,command.InitCommand.BlurP2.Y)
                });
                bindContext
                    .Push(new Push
                    {
                        BufferAddress = buffer.GetAddress(),
                        IsHorizontal = 1
                    })
                    .Draw(6);
            }
            ctx.EndRendering();
        }
    }
}
internal class BlurSecondPassCommandHandler : ICommandHandler
{
        private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("Framework/Shaders/Views/blur.slang");
    private BlurSecondPassCommand[] _commands = [];
    
    private uint[] _bufferIds = [];
    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<BlurSecondPassCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        foreach (var command in _commands)
        {
            config.ReadTexture(command.InitCommand.SecondPassImageId, ImageLayout.ShaderReadOnly);
        }
        _bufferIds = Enumerable.Range(0, _commands.Length)
            .Select(_ => config.CreateBuffer<BlurData>(GraphBufferUsage.HostThenGraphics)).ToArray();
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        Debug.Assert(passConfig is MainPassConfig);
        
        foreach (var (command,bufferId) in _commands.Zip(_bufferIds))
        {
            var srcImage = graph.GetTexture(command.InitCommand.SecondPassImageId);
            var buffer = graph.GetBufferOrException(bufferId);
            if (_shader.Bind(ctx) is { } bindContext)
            {
                buffer.Write(new BlurData
                {
                    SourceT = srcImage.Handle,
                    Projection = surfaceContext.ProjectionMatrix,
                    Size = command.InitCommand.Size,
                    Strength = command.InitCommand.Strength,
                    Radius = command.InitCommand.BlurRadius,
                    Tint = command.InitCommand.Tint,
                    Transform = command.InitCommand.Transform,
                    DestRect = new Vector4(command.InitCommand.BoundingBoxP1,command.InitCommand.BoundingBoxP2.X,command.InitCommand.BoundingBoxP2.Y)
                });
                // buffer.Write(new BlurData
                // {
                //     SourceT = srcImage.BindlessHandle,
                //     Projection = surfaceContext.ProjectionMatrix,
                //     Size = command.InitCommand.Size,
                //     Strength = 1f,
                //     Radius = Vector2.One,
                //     Tint = command.InitCommand.Tint,
                //     Transform = command.InitCommand.Transform,
                //     DestRect = new Vector4(command.InitCommand.BoundingBoxP1,command.InitCommand.BoundingBoxP2.X,command.InitCommand.BoundingBoxP2.Y)
                // });
                bindContext
                    .Push(new Push
                    {
                        BufferAddress = buffer.GetAddress(),
                        IsHorizontal = 0
                    })
                    .Draw(6);
            }
        }
    }
}

