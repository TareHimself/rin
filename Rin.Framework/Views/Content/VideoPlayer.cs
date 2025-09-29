using System.Numerics;
using Rin.Framework.Animation;
using Rin.Framework.Math;
using Rin.Framework.Views.Graphics.Blur;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Graph;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Video;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.PassConfigs;

namespace Rin.Framework.Views.Content;

internal class VideoCommand(in Matrix4x4 transform, in Vector2 size, Buffer<byte> frameData, in Extent2D extent)
    : TCommand<MainPassConfig, VideoCommandHandler>
{
    public readonly Buffer<byte> FrameData = frameData;
    public Extent2D Extent = extent;
    public Vector2 Size = size;
    public Matrix4x4 Transform = transform;
}

internal class CreateVideoResourcesPass(VideoCommand[] commands) : IPass
{
    public uint[] VideoImageFrameIds = [];
    public uint[] VideoStagingBufferIds = [];
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public Action? OnPrune { get; } = () =>
    {
        foreach (var command in commands)
        {
            command.FrameData.Dispose();
        }
    };

    public void Configure(IGraphConfig config)
    {
        VideoStagingBufferIds = commands
            .Select(c => config.CreateBuffer(c.FrameData.GetByteSize(), GraphBufferUsage.HostThenTransfer)).ToArray();
        VideoImageFrameIds = commands
            .Select(c => config.CreateTexture(c.Extent, ImageFormat.RGBA8, ImageLayout.TransferDst)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var stagingBuffers = VideoStagingBufferIds.Select(graph.GetBufferOrException);
        var images = VideoImageFrameIds.Select(graph.GetTexture);

        foreach (var (stagingBuffer, image, cmd) in stagingBuffers.Zip(images, commands))
        {
            stagingBuffer.Write(cmd.FrameData);
            ctx.CopyToImage(stagingBuffer, image);
        }

        foreach (var cmd in commands) cmd.FrameData.Dispose();
    }
}

internal class VideoCommandHandler : ICommandHandlerWithPreAdd
{
    private readonly IGraphicsShader _shader = IGraphicsModule.Get()
        .MakeGraphics("Framework/Shaders/Views/video.slang");

    private VideoCommand[] _commands = [];
    private uint _itemBufferId;

    private CreateVideoResourcesPass _resourcesPass = null!;

    public void PreAdd(IGraphBuilder builder)
    {
        builder.AddPass(_resourcesPass);
    }

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<VideoCommand>().ToArray();
        _resourcesPass = new CreateVideoResourcesPass(_commands);
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _itemBufferId = config.CreateBuffer<VideoItem>(_resourcesPass.VideoImageFrameIds.Length,
            GraphBufferUsage.HostThenGraphics);
        foreach (var id in _resourcesPass.VideoImageFrameIds) config.ReadTexture(id, ImageLayout.ShaderReadOnly);
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_shader.Bind(ctx) is { } bindContext)
        {
            var frameImages = _resourcesPass.VideoImageFrameIds.Select(graph.GetTexture).ToArray();
            var buffer = graph.GetBufferOrException(_itemBufferId);

            buffer.Write(_commands.Select((c, idx) => new VideoItem
            {
                Transform = c.Transform,
                Size = c.Size,
                FrameHandle = frameImages[idx].Handle
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

                bindContext.Push(new Push
                {
                    Projection = surfaceContext.ProjectionMatrix,
                    ItemBufferAddress = buffer.GetAddress() + offset
                });
                bindContext.Draw(6);
                offset += itemSize;
            }
        }
    }

    private struct Push
    {
        public required Matrix4x4 Projection;
        public required ulong ItemBufferAddress;
    }

    private struct VideoItem
    {
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required ImageHandle FrameHandle;
    }
}

public class VideoPlayer : ContentView
{
    private readonly IVideoPlayer _player;

    private Matrix4x4 _lastCollectAbsoluteTransform = Matrix4x4.Identity;

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
        return FromSource(new FileVideoSource(fileName));
    }
    
    public static VideoPlayer FromSource(IVideoSource source)
    {
        var player = new WebmVideoPlayer();
        player.SetSource(source);
        return new VideoPlayer(player);
    }
    

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace.FiniteOr(Vector2.Zero);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (_player.HasVideo)
        {
            var extent = _player.VideoExtent;
            return new Vector2(extent.Width, extent.Height);
        }

        return Vector2.Zero;
    }
    
    // protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    // {
    //     base.OnCursorEnter(e);
    //     this.StopAll().Transition(() => blurRadius,(d) => blurRadius = d,140.0f,1.0f);
    // }
    //
    // protected override void OnCursorLeave()
    // {
    //     base.OnCursorLeave();
    //     this.StopAll().Do(() => blurRadius = 0f);
    // }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        _lastCollectAbsoluteTransform = transform;
        if (_player.HasVideo)
        {
            var buff = _player.CopyRecentFrame();
            commands.Add(new VideoCommand(transform, GetContentSize(), buff, _player.VideoExtent));
        }

        const int barSize = 10;
        var contentSize = GetContentSize();
        var barOffset = contentSize with { X = 0 };
        barOffset.Y -= barSize;
        var barTransform = transform.Translate(barOffset);
        
        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.Position / _player.Duration), barSize), Color.White);
        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.DecodedPosition / _player.Duration), barSize),
            Color.White with { A = 0.5f });

        if (_player.HasVideo && IsHovered)
        {
            var size = new Vector2(200);
            commands.AddBlur(Matrix4x4.Identity.Translate(_cursorPosition - (size / 2)), size,radius: 15);
        }
    }
    
    private Vector2 _cursorPosition = Vector2.Zero;
    protected override bool OnCursorMove(CursorMoveSurfaceEvent e)
    {
        _cursorPosition = e.Position;
        return base.OnCursorMove(e);
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
                _player.Pause();
            else
                _player.Play();
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