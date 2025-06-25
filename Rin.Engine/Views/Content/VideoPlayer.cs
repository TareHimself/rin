using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Video;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Content;

internal class VideoCommand(in Matrix4x4 transform,in Vector2 size,Buffer<byte> frameData,in Extent2D extent) : TCommand<VideoPass>
{
    public Matrix4x4 Transform = transform;
    public Vector2 Size = size;
    public readonly Buffer<byte> FrameData = frameData;
    public Extent2D Extent = extent;
}

internal class CreateVideoResourcesPass(VideoCommand[] commands) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public uint[] VideoImageFrameIds = [];
    public uint[] VideoStagingBufferIds = [];
    public void Configure(IGraphConfig config)
    {
        VideoStagingBufferIds = commands
            .Select(c => config.CreateBuffer(c.FrameData.GetByteSize(), GraphBufferUsage.HostThenTransfer)).ToArray();
        VideoImageFrameIds = commands
            .Select(c => config.CreateImage(c.Extent, ImageFormat.RGBA8, ImageLayout.TransferDst)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var stagingBuffers = VideoStagingBufferIds.Select(graph.GetBufferOrException);
        var images = VideoImageFrameIds.Select(graph.GetImageOrException);
        
        foreach (var (stagingBuffer, image,cmd) in stagingBuffers.Zip(images,commands))
        {
            stagingBuffer.WriteBuffer(cmd.FrameData);
            ctx.CopyToImage(image, stagingBuffer);
        }
        foreach (var cmd in commands)
        {
            cmd.FrameData.Dispose();
        }
    }
}

internal class VideoPass(SurfacePassContext passContext,VideoCommand[] commands) : IViewsPass, IWithPreAdd
{
    public uint Id { get; set; }
    public bool IsTerminal => false;
    
    private readonly IGraphicsShader _shader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/video.slang");
    private readonly CreateVideoResourcesPass _resourcesPass = new CreateVideoResourcesPass(commands);
    private uint _itemBufferId = 0;
    public void Configure(IGraphConfig config)
    {
        _itemBufferId = config.CreateBuffer<VideoItem>(_resourcesPass.VideoImageFrameIds.Length,
            GraphBufferUsage.HostThenGraphics);
        foreach (var id in _resourcesPass.VideoImageFrameIds)
        {
            config.ReadImage(id, ImageLayout.ShaderReadOnly);
        }
        config.WriteImage(passContext.MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(passContext.StencilImageId, ImageLayout.StencilAttachment);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx))
        {
            var mainImage = graph.GetImageOrException(passContext.MainImageId);
            var stencilImage = graph.GetImageOrException(passContext.StencilImageId);
            var frameImages = _resourcesPass.VideoImageFrameIds.Select(graph.GetImageOrException).ToArray();
            var buffer = graph.GetBufferOrException(_itemBufferId);
        
            buffer.WriteArray(commands.Select((c,idx) => new VideoItem
            {
                Transform = c.Transform,
                Size = c.Size,
                FrameHandle = frameImages[idx].BindlessHandle
            }));
        
            ctx.BeginRendering(passContext.Extent, [mainImage], stencilAttachment: stencilImage)
                .DisableFaceCulling()
                .StencilCompareOnly();

            var compareMask = uint.MaxValue;

            ulong offset = 0;
            var itemSize = Utils.ByteSizeOf<VideoItem>();
            for (var i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                var currentCompareMask = command.StencilMask;
                if (currentCompareMask != compareMask)
                {
                    compareMask = currentCompareMask;
                    ctx.SetStencilCompareMask(compareMask);
                }
                _shader.Push(ctx,new Push
                {
                    Projection = passContext.ProjectionMatrix,
                    ItemBufferAddress = buffer.GetAddress() + offset
                });
                ctx.Draw(6, 1);
                offset += itemSize;
            }

            ctx.EndRendering();
        }
    }

    public void PreAdd(IGraphBuilder builder)
    {
        builder.AddPass(_resourcesPass);
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        var asVideoCommands = info.Commands.Cast<VideoCommand>().ToArray();
        return new VideoPass(info.Context, asVideoCommands);
    }

    struct Push
    {
        public required Matrix4x4 Projection;
        public required ulong ItemBufferAddress;
    }

    struct VideoItem
    {
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required ImageHandle FrameHandle;
    }
}
public class VideoPlayer : ContentView
{
    private IVideoPlayer _player;
    
    private VideoPlayer(IVideoPlayer context)
    {
        _player = context;
    }

    public override void Dispose()
    {
        base.Dispose();
        _player.Dispose();
    }

    public static VideoPlayer FromFile(string fileName)
    {
        var player = new WebmVideoPlayer();
        player.SetSource( new FileSource(fileName));
        return new VideoPlayer(player);
    }
    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace.FiniteOr(Vector2.Zero);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (_player.HasVideo)
        {
            var extent = _player.VideoExtent;
            return new Vector2(extent.Width, extent.Height);
        }

        return Vector2.Zero;
    }
    
    private Matrix4x4 _lastCollectAbsoluteTransform = Matrix4x4.Identity;

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        _lastCollectAbsoluteTransform = transform;
        if (_player.HasVideo)
        {
            var buff = _player.CopyRecentFrame();
            commands.Add(new VideoCommand(transform,GetContentSize(),buff,_player.VideoExtent));
        }
        
        var barSize = 10;
        var contentSize = GetContentSize();
        var barOffset = contentSize with{ X = 0 };
        barOffset.Y -= barSize;
        var barTransform = transform.Translate(barOffset);

        if (IsHovered)
        {
            commands.AddText(transform, "Noto Sans", "Hovered");
        }
        commands.AddRect(barTransform,new Vector2(contentSize.X * (float)(_player.Position / _player.Duration),barSize),Color.White);
        commands.AddRect(barTransform,new Vector2(contentSize.X * (float)(_player.DecodedPosition / _player.Duration),barSize),Color.White with{ A = 0.5f});
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _player.TryDecode();
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        if (e.Button is CursorButton.One)
        {
            if (_player.IsPlaying)
            {
                _player.Pause();
            }
            else
            {
                _player.Play();
            }
            return true;
        }

        if (e.Button is CursorButton.Two)
        {
            var localPosition = e.Position.Transform(_lastCollectAbsoluteTransform.Inverse());
            var size = GetContentSize();
            var percent = localPosition.X / size.X;
            var time = _player.Duration * percent;
            _player.Seek(time);
            return true;
        }
        return base.OnCursorDown(e);
    }
}