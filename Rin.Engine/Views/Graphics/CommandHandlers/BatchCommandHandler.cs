using System.Diagnostics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;
using Rin.Engine.Views.Graphics.PassConfigs;

namespace Rin.Engine.Views.Graphics.CommandHandlers;

public class BatchCommandHandler : ICommandHandler
{
    
    private readonly List<IBatch> _batches = [];
    private readonly List<uint> _masks = [];
    private ulong[] _batchSizes = [];
    private uint _bufferId;

    public void Init(ICommand[] commands)
    {
        foreach (var cmd in commands.Cast<IBatchedCommand>())
        {
            var canBeBatchedWithPrevious = _batches.LastOrDefault() is { } batch && batch.GetBatcher() == cmd.GetBatcher() &&
                                           _masks.Last() == cmd.StencilMask;
            if (canBeBatchedWithPrevious)
            {
                _batches.Last().AddFromCommand(cmd);
            }
            else
            {
                var newBatch = cmd.GetBatcher().NewBatch();
                newBatch.AddFromCommand(cmd);
                _batches.Add(newBatch);
                _masks.Add(cmd.StencilMask);
            }
        }
        
        _batchSizes = _batches.Select(c => c.GetMemoryNeeded().Aggregate((a, b) => a + b)).ToArray();
    }

    public void Configure(IGraphConfig config, IPassConfig passConfig)
    {
        Debug.Assert(passConfig is MainPassConfig);
        
        var memoryNeeded = _batchSizes.Aggregate<ulong, ulong>(0, (t, c) => t + c);

        if (memoryNeeded > 0) _bufferId = config.CreateBuffer(memoryNeeded, GraphBufferUsage.HostThenGraphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx, IPassConfig passConfig)
    {
        Debug.Assert(passConfig is MainPassConfig);
        var viewFrame = new ViewsFrame(passConfig.PassContext, ctx);
        var buffer = graph.GetBufferOrNull(_bufferId);
        
        var compareMask = uint.MaxValue;

        ulong offset = 0;
        for (var i = 0; i < _batches.Count; i++)
        {
            var batch = _batches[i];
            var mask = _masks[i];
            if (mask != compareMask)
            {
                compareMask = mask;
                ctx.SetStencilCompareMask(compareMask);
            }

            var batcher = batch.GetBatcher();
            var bufferSize = _batchSizes[i];
            batcher.Draw(viewFrame, batch, buffer.GetView(offset, bufferSize));
            offset += bufferSize;
        }
    }
}