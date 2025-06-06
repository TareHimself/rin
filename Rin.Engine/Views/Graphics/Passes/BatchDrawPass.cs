using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.Passes;

public sealed class BatchDrawPass : IViewsPass
{
    private readonly List<Pair<IBatch, uint>> _batches;
    private readonly ulong[] _batchSizes;
    private readonly Extent2D _extent;

    private readonly List<Pair<ulong, ulong>> _offsets = [];

    // private readonly PassInfo _passInfo;
    private uint _bufferId;
    private FrameStats _stats;

    public BatchDrawPass(PassCreateInfo createInfo)
    {
        Context = createInfo.Context;
        var commands = createInfo.Commands.Cast<IBatchedCommand>();
        _extent = Context.Extent;

        _batches = commands.Aggregate(new List<Pair<IBatch, uint>>(), (batches, cmd) =>
        {
            var isSameBatcher = batches.LastOrDefault() is { } batch && batch.First.GetBatcher() == cmd.GetBatcher() &&
                                batch.Second == cmd.StencilMask;
            if (isSameBatcher)
            {
                batches.Last().First.AddFromCommand(cmd);
            }
            else
            {
                var newBatch = cmd.GetBatcher().NewBatch();
                newBatch.AddFromCommand(cmd);
                batches.Add(new Pair<IBatch, uint>(newBatch, cmd.StencilMask));
            }

            return batches;
        });
        _batchSizes = _batches.Select(c => c.First.GetMemoryNeeded().Aggregate((a, b) => a + b)).ToArray();
    }

    [PublicAPI] public uint MainImageId { get; set; }

    [PublicAPI] public uint CopyImageId { get; set; }

    [PublicAPI] public uint StencilImageId { get; set; }

    public string Name => "View Pass";

    public SharedPassContext Context { get; set; }

    [PublicAPI] public bool IsTerminal { get; set; }

    public void Configure(IGraphConfig config)
    {
        MainImageId = config.WriteImage(Context.MainImageId, ImageLayout.ColorAttachment);
        StencilImageId = config.ReadImage(Context.StencilImageId, ImageLayout.StencilAttachment);

        var memoryNeeded = _batchSizes.Aggregate<ulong, ulong>(0, (t, c) => t + c);

        if (memoryNeeded > 0) _bufferId = config.CreateBuffer(memoryNeeded, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetImage(MainImageId);
        var stencilImage = graph.GetImage(StencilImageId);
        var buffer = graph.GetBufferOrNull(_bufferId);

        var viewFrame = new ViewsFrame(Context, ctx);

        // foreach (var command in _passInfo.PreCommands) command.Execute(viewFrame);

        ctx.BeginRendering(_extent, [drawImage], stencilAttachment: stencilImage)
            .DisableFaceCulling()
            .StencilCompareOnly();

        var compareMask = uint.MaxValue;

        ulong offset = 0;
        for (var i = 0; i < _batches.Count; i++)
        {
            var (batch, currentCompareMask) = _batches[i];
            if (currentCompareMask != compareMask)
            {
                compareMask = currentCompareMask;
                ctx.SetStencilCompareMask(compareMask);
            }

            var batcher = batch.GetBatcher();
            var bufferSize = _batchSizes[i];
            var view = bufferSize > 0 ? buffer?.GetView(offset, bufferSize) : null;
            batcher.Draw(viewFrame, batch, view);
            offset += bufferSize;
        }

        ctx.EndRendering();
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new BatchDrawPass(info);
    }

    public uint Id { get; set; }
}