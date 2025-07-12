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
using Rin.Engine.Views.Graphics.CommandHandlers;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.PassConfigs;
using Rin.Engine.Views.Graphics.Quads;

namespace Rin.Engine.Views.Content;

internal class VideoCommand(in Matrix4x4 transform,in Vector2 size,Buffer<byte> frameData,in Extent2D extent) : TCommand<MainPassConfig,VideoCommandHandler>
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

internal class VideoCommandHandler : ICommandHandlerWithPreAdd
{
    private readonly IGraphicsShader _shader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/video.slang");

    private CreateVideoResourcesPass _resourcesPass = null!;
    private uint _itemBufferId = 0;

    private VideoCommand[] _commands = [];
    public void PreAdd(IGraphBuilder builder)
    {
        builder.AddPass(_resourcesPass);
    }
    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<VideoCommand>().ToArray();
        _resourcesPass = new CreateVideoResourcesPass(_commands);
    }

    public void Configure(IGraphConfig config, IPassConfig passConfig)
    {
        _itemBufferId = config.CreateBuffer<VideoItem>(_resourcesPass.VideoImageFrameIds.Length,
            GraphBufferUsage.HostThenGraphics);
        foreach (var id in _resourcesPass.VideoImageFrameIds)
        {
            config.ReadImage(id, ImageLayout.ShaderReadOnly);
        }
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx, IPassConfig passConfig)
    {
        if (_shader.Bind(ctx))
        {
            var frameImages = _resourcesPass.VideoImageFrameIds.Select(graph.GetImageOrException).ToArray();
            var buffer = graph.GetBufferOrException(_itemBufferId);
        
            buffer.WriteArray(_commands.Select((c,idx) => new VideoItem
            {
                Transform = c.Transform,
                Size = c.Size,
                FrameHandle = frameImages[idx].BindlessHandle
            }));
        
            var compareMask = uint.MaxValue;

            ulong offset = 0;
            var itemSize = Utils.ByteSizeOf<VideoItem>();
            for (var i = 0; i < _commands.Length; i++)
            {
                var command = _commands[i];
                var currentCompareMask = command.StencilMask;
                if (currentCompareMask != compareMask)
                {
                    compareMask = currentCompareMask;
                    ctx.SetStencilCompareMask(compareMask);
                }
                _shader.Push(ctx,new Push
                {
                    Projection = passConfig.PassContext.ProjectionMatrix,
                    ItemBufferAddress = buffer.GetAddress() + offset
                });
                ctx.Draw(6, 1);
                offset += itemSize;
            }
        }
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