using Rin.Core.Extensions;
using System.Buffers;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Video;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Core.Views.Content;

internal class VideoCommand(in Matrix4x4 transform, in Vector2 size, Buffer<byte> frameData, in Extent2D extent)
    : TCommand<MainPassConfig, VideoCommandHandler>
{
    public readonly Buffer<byte> FrameData = frameData;
    public readonly VideoFramePlanes Planes = IVideoPlayer.PlanesOf(extent);
    // The container does not carry the colour matrix, so HD is taken as BT.709 and SD as BT.601.
    public readonly VideoColorimetry Colorimetry = extent.Height >= 720 ? VideoColorimetry.Bt709 : VideoColorimetry.Bt601;
    public Vector2 Size = size;
    public Matrix4x4 Transform = transform;
}

/// <summary>
///     Uploads each command's NV12 frame into a luma (R8) and a chroma (RG8) texture from one staging buffer.
/// </summary>
internal class CreateVideoResourcesPass(VideoCommand[] commands) : IPass, IDisposable
{
    public uint[] LumaImageIds = [];
    public uint[] ChromaImageIds = [];
    private uint[] _videoStagingBufferIds = [];
    public uint Id { get; set; }

    // Whether this pass ran or got pruned, the decoded frame data behind it is owned here and
    // released exactly once - the graph calls Dispose in either case.
    public void Dispose()
    {
        foreach (var command in commands) command.FrameData.Dispose();
    }

    public void Configure(IGraphConfig config)
    {
        _videoStagingBufferIds = commands
            .Select(c => config.CreateBuffer(c.FrameData.GetByteSize(), GraphBufferUsage.HostThenTransfer)).ToArray();
        LumaImageIds = commands
            .Select(c => config.CreateTexture(c.Planes.Extent, ImageFormat.R8, ImageLayout.TransferDst)).ToArray();
        ChromaImageIds = commands
            .Select(c => config.CreateTexture(c.Planes.ChromaExtent, ImageFormat.RG8, ImageLayout.TransferDst)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        for (var i = 0; i < commands.Length; i++)
        {
            var command = commands[i];
            var planes = command.Planes;
            var stagingBuffer = graph.GetBufferOrException(_videoStagingBufferIds[i]);
            stagingBuffer.Write(command.FrameData);
            ctx.CopyToImage(stagingBuffer.GetView(0, planes.LumaByteSize), graph.GetImage(LumaImageIds[i]));
            ctx.CopyToImage(stagingBuffer.GetView(planes.LumaByteSize, planes.ChromaByteSize),
                graph.GetImage(ChromaImageIds[i]));
        }
    }
}

internal partial class VideoCommandHandler : ICommandHandlerWithPreAdd
{
    [GraphicsShader<VideoBatchShader>]
    private partial IGraphicsShader VideoShader
    {
        get;
    }

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
        _itemBufferId = config.CreateBuffer<VideoItem>(_commands.Length, GraphBufferUsage.HostThenGraphics);
        foreach (var id in _resourcesPass.LumaImageIds) config.ReadTexture(id, ImageLayout.ShaderReadOnly);
        foreach (var id in _resourcesPass.ChromaImageIds) config.ReadTexture(id, ImageLayout.ShaderReadOnly);
    }

    private int MaxStackVideoItems = 5;
    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        if (VideoShader.Bind(ctx) is { } bindContext)
        {
            var buffer = graph.GetBufferOrException(_itemBufferId);

            {
                var rented = _commands.Length > MaxStackVideoItems
                    ? ArrayPool<VideoItem>.Shared.Rent(_commands.Length)
                    : null;
                try
                {
                    var videoItems = rented is not null
                        ? rented.AsSpan(0, _commands.Length)
                        : stackalloc VideoItem[_commands.Length];
                    for(var i = 0; i < _commands.Length; i++)
                    {
                        var command = _commands[i];
                        videoItems[i].Transform = command.Transform;
                        videoItems[i].Size = command.Size;
                        videoItems[i].Luma = graph.GetImage(_resourcesPass.LumaImageIds[i]);
                        videoItems[i].Chroma = graph.GetImage(_resourcesPass.ChromaImageIds[i]);
                        videoItems[i].Colorimetry = command.Colorimetry;
                    }
                    buffer.Write(videoItems);
                }
                finally
                {
                    if(rented is not null) ArrayPool<VideoItem>.Shared.Return(rented);
                }
            }
            
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

                bindContext.Push(new VideoBatchShader.PushConstants
                {
                    Projection = surfaceContext.ProjectionMatrix,
                    Items = new BufferRef<VideoItem>(buffer.GetAddress() + offset)
                });
                bindContext.Draw(6);
                offset += itemSize;
            }
        }
    }
}

public class VideoPlayerView : ContentView
{
    private readonly IVideoPlayer _player;
    private bool _laidOutForVideo;

    private VideoPlayerView(IVideoPlayer context)
    {
        _player = context;
    }

    public bool IsReady => _player.IsReady;
    public bool HasFailed => _player.HasFailed;
    public Exception? Error => _player.Error;
    public bool HasVideo => _player.HasVideo;
    public bool IsPlaying => _player.IsPlaying;
    public double Position => _player.Position;
    public double Duration => _player.Duration;

    public void Play()
    {
        _player.Play();
    }

    public void Pause()
    {
        _player.Pause();
    }

    public override void Dispose()
    {
        base.Dispose();
        _player.Dispose();
    }

    public static VideoPlayerView FromFile(string fileName)
    {
        return FromSource(new FileVideoSource(fileName));
    }

    public static VideoPlayerView FromUrl(Uri url)
    {
        return FromSource(new HttpVideoSource(url));
    }

    public static VideoPlayerView FromSource(IVideoSource source)
    {
        var player = new WebmVideoPlayer();
        player.SetSource(source);
        return new VideoPlayerView(player);
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
        if (_player.HasVideo)
        {
            var buff = _player.CopyRecentFrame();
            commands.Add(new VideoCommand(transform, GetContentSize(), buff, _player.VideoExtent));
        }

        // A live stream has no duration, so there is no progress to show.
        if (_player.Duration <= 0.0) return;

        const int barSize = 10;
        var contentSize = GetContentSize();
        var barOffset = contentSize with { X = 0 };
        barOffset.Y -= barSize;
        var barTransform = transform.ApplyBefore(Matrix4x4.Identity.Translate(barOffset));

        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.Position / _player.Duration), barSize), Color.White);
        commands.AddRect(barTransform,
            new Vector2(contentSize.X * (float)(_player.DecodedPosition / _player.Duration), barSize),
            Color.White with { A = 0.5f });
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _player.TryDecode();
        // The extent is unknown until the headers are read, which happens on the decode thread.
        if (_laidOutForVideo || !_player.IsReady) return;
        _laidOutForVideo = true;
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button is CursorButton.One)
        {
            if (_player.IsPlaying)
                _player.Pause();
            else
                _player.Play();
            e.Target = this;
            return;
        }

        if (e.Button is CursorButton.Two)
        {
            var localPosition = e.Position.Transform(transform.Inverse());
            var size = GetSize();
            var percent = localPosition.X / size.X;
            if (_player.Duration <= 0.0) return;
            _player.Seek(_player.Duration * percent);
            e.Target = this;
        }
    }
}
