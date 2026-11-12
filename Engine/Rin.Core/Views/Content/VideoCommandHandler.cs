using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared.Pooling;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Core.Views.Content;

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
                using var scratch = new ScratchSpan<VideoItem>(_commands.Length,
                    _commands.Length <= MaxStackVideoItems ? stackalloc VideoItem[_commands.Length] : default);
                var videoItems = scratch.Span;
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
                    Items = buffer.GetView(offset, buffer.Size - offset).ToDeviceSpan<VideoItem>()
                });
                bindContext.Draw(6);
                offset += itemSize;
            }
        }
    }
}
