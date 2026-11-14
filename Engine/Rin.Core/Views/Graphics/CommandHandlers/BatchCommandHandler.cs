using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Rin.Core.Views.Graphics.CommandHandlers;

public class BatchCommandHandler : ICommandHandler, IDisposable
{
    private readonly List<IBatch> _batches = [];
    private readonly List<uint> _masks = [];
    private ulong[] _batchSizes = [];
    private ulong _memoryNeeded;
    private uint _bufferId;

    public void Init(ReadOnlySpan<ICommand> commands)
    {
        foreach (var command in commands)
        {
            var cmd = (IBatchedCommand)command;
            var last = _batches.Count - 1;
            var canBeBatchedWithPrevious = last >= 0 &&
                                           _batches[last].GetBatcher() == cmd.GetBatcher() &&
                                           _masks[last] == cmd.StencilMask;
            if (canBeBatchedWithPrevious)
            {
                _batches[last].AddFromCommand(cmd);
            }
            else
            {
                var newBatch = cmd.GetBatcher().NewBatch();
                newBatch.AddFromCommand(cmd);
                _batches.Add(newBatch);
                _masks.Add(cmd.StencilMask);
            }
        }

        _batchSizes = new ulong[_batches.Count];
        for (var i = 0; i < _batchSizes.Length; i++)
        {
            _batchSizes[i] = _batches[i].GetMemoryNeeded();
            _memoryNeeded += _batchSizes[i];
        }
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        Debug.Assert(passConfig is MainPassConfig);

        if (_memoryNeeded > 0) _bufferId = config.CreateBuffer(_memoryNeeded, GraphBufferUsage.HostThenGraphics);
        foreach (var batch in _batches) batch.DeclareResources(config);
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        Debug.Assert(passConfig is MainPassConfig);
        var viewFrame = new ViewsFrame(surfaceContext, ctx);
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

    public void Dispose()
    {
        foreach (var batch in _batches) (batch as IDisposable)?.Dispose();
        _batches.Clear();
    }
}