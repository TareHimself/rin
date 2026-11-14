using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.Passes;
using Rin.Core.Extensions;
using Rin.Core.Shared.Pooling;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Graphics;

public class DefaultCollectedSurfaceData : ICollectedSurfaceData, IDisposable
{
    private const int MaxStackStencilClips = 8;
    private readonly CommandList _commandList;
    private readonly Recycler<CommandList>? _commandLists;
    private bool _disposed;
    private bool _written;

    public DefaultCollectedSurfaceData(CommandList commandList, Recycler<CommandList>? commandLists = null)
    {
        _commandList = commandList;
        _commandLists = commandLists;
        var size = _commandList.SurfaceSize.Ceiling();
        SurfaceContext = new SurfaceContext(new Extent2D
        {
            Width = (uint)size.X,
            Height = (uint)size.Y
        });
    }

    public virtual void Write(IGraphBuilder builder)
    {
        _written = true;
        var drawList = _commandList;

        var clips = drawList.Clips;

        builder.AddPass(new CreateImagesPass(SurfaceContext));
        {
            var uniqueClipStacks = drawList.UniqueClipStacks;
            Dictionary<ClipStackKey, uint> computedClipMasks = [];
            List<ICommand> pendingCommands = [];
            Span<StencilClip> stackClips = stackalloc StencilClip[MaxStackStencilClips];
            uint shifted = 1;
            uint currentMask = 0x2;
            for (var i = 0; i < drawList.Commands.Count; i++)
            {
                var command = drawList.Commands[i];
                var clipId = drawList.ClipIds[i];

                if (shifted == 8)
                {
                    ProcessPendingCommands(pendingCommands, SurfaceContext, builder);
                    pendingCommands.Clear();
                    computedClipMasks.Clear();
                    currentMask = 0x02;
                    shifted = 1;
                    builder.AddPass(new StencilClearPass(SurfaceContext));
                }


                if (clipId.Ids.Length == 0) // No clipping
                {
                    command.StencilMask = 0x01;
                    pendingCommands.Add(command);
                }
                else if (computedClipMasks.TryGetValue(clipId, out var clipMask))
                {
                    command.StencilMask = clipMask;
                    pendingCommands.Add(command);
                }
                else
                {
                    var stackIds = uniqueClipStacks[clipId];
                    using var scratch = new ScratchSpan<StencilClip>(stackIds.Length, stackClips);
                    BuildStencilClips(stackIds, clips, scratch.Span);
                    builder.AddPass(new StencilWritePass(SurfaceContext, currentMask, scratch.Span));
                    computedClipMasks.Add(clipId, currentMask);
                    command.StencilMask = currentMask;
                    pendingCommands.Add(command);
                    currentMask <<= 1;
                    shifted++;
                }
            }

            if (pendingCommands.Count != 0)
            {
                ProcessPendingCommands(pendingCommands, SurfaceContext, builder);
                pendingCommands.Clear();
            }
        }
    }

    public SurfaceContext SurfaceContext { get; }

    /// <summary>
    ///     Releases the commands of data that was never written (once written, its passes release them), then
    ///     hands the command list back for the next frame.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_written)
            foreach (var command in _commandList.Commands)
                (command as IDisposable)?.Dispose();
        _commandLists?.Return(_commandList);
    }

    private static void BuildStencilClips(uint[] clipIds, List<ClipInfo> clips, Span<StencilClip> into)
    {
        for (var i = 0; i < clipIds.Length; i++)
        {
            var clip = clips[(int)clipIds[i]];
            into[i] = new StencilClip(clip.Transform, clip.Size);
        }
    }

    
    private static void ProcessPendingCommands(IEnumerable<ICommand> drawCommands,
        SurfaceContext context, IGraphBuilder builder)
    {
        List<ICommand> currentCommands = [];
        List<ICommandHandler> currentHandlers = [];
        Type? currentPassConfigType = null;
        Type? currentHandlerType = null;

        foreach (var cmd in drawCommands)
        {
            // NoOpCommand's are like breaks so we break any batching that can happen here
            if (cmd is NoOpCommand && currentCommands.NotEmpty())
            {
                currentHandlers.Add(currentCommands[0].CreateHandler(CollectionsMarshal.AsSpan(currentCommands)));

                builder.AddPass(new ViewsDrawPass(context, currentCommands[0].CreateConfig(context),
                    CollectionsMarshal.AsSpan(currentHandlers)));

                currentCommands.Clear();
                currentHandlers.Clear();
                continue;
            }

            if (currentCommands.Count > 0)
            {
                var firstCmd = currentCommands[0];
                if (currentHandlerType != cmd.HandlerType)
                {
                    currentHandlers.Add(firstCmd.CreateHandler(CollectionsMarshal.AsSpan(currentCommands)));
                    currentCommands.Clear();
                }

                if (currentPassConfigType != cmd.PassConfigType)
                {
                    builder.AddPass(new ViewsDrawPass(context, firstCmd.CreateConfig(context),
                        CollectionsMarshal.AsSpan(currentHandlers)));
                    currentHandlers.Clear();
                }
            }

            currentCommands.Add(cmd);
            currentHandlerType = cmd.HandlerType;
            currentPassConfigType = cmd.PassConfigType;
        }

        if (currentCommands.NotEmpty())
        {
            currentHandlers.Add(currentCommands[0].CreateHandler(CollectionsMarshal.AsSpan(currentCommands)));
            builder.AddPass(new ViewsDrawPass(context, currentCommands[0].CreateConfig(context),
                CollectionsMarshal.AsSpan(currentHandlers)));
        }
    }
}
