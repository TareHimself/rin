using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Math;
using Rin.Framework.Views.Graphics.CommandHandlers;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Views.Graphics.Passes;

namespace Rin.Framework.Views.Graphics;

public class DefaultCollectedSurfaceData : ICollectedSurfaceData
{
    private CommandList _commandList;

    public DefaultCollectedSurfaceData(CommandList commandList)
    {
        _commandList = commandList;
        var size = _commandList.SurfaceSize.Ceiling();
        SurfaceContext = new SurfaceContext(new Extent2D
        {
            Width = (uint)size.X,
            Height = (uint)size.Y
        });
    }

    public virtual void Write(IGraphBuilder builder)
    {
        var drawList = _commandList;
        
        var clips = drawList.Clips;

        List<IPass> passes = [new CreateImagesPass(SurfaceContext)];
        {
            var uniqueClipStacks = drawList.UniqueClipStacks;
            Dictionary<string, uint> computedClipMasks = [];
            List<ICommand> pendingCommands = [];
            uint shifted = 1;
            uint currentMask = 0x2;
            foreach (var (command, clipId) in drawList.Commands.Zip(drawList.ClipIds))
            {
                if (shifted == 31)
                {
                    ProcessPendingCommands(pendingCommands, SurfaceContext, passes);
                    pendingCommands.Clear();
                    computedClipMasks.Clear();
                    currentMask = 0x02;
                    shifted = 1;
                    passes.Add(new StencilClearPass(SurfaceContext));
                }


                if (clipId.Length <= 0) // No clipping
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
                    passes.Add(new StencilWritePass(SurfaceContext, currentMask,
                        uniqueClipStacks[clipId]
                            .Select(c => new StencilClip(clips[(int)c].Transform, clips[(int)c].Size)).ToArray()));
                    computedClipMasks.Add(clipId, currentMask);
                    command.StencilMask = currentMask;
                    pendingCommands.Add(command);
                    currentMask <<= 1;
                    shifted++;
                }
            }

            if (pendingCommands.Count != 0)
            {
                ProcessPendingCommands(pendingCommands, SurfaceContext, passes);
                pendingCommands.Clear();
            }
        }

        foreach (var pass in passes) builder.AddPass(pass);
    }
    
    private void ProcessPendingCommands(IEnumerable<ICommand> drawCommands,
        SurfaceContext context, List<IPass> passes)
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
                if (currentCommands.Count > 0)
                    currentHandlers.Add(currentCommands.First().CreateHandler(currentCommands.ToArray()));

                passes.Add(new ViewsDrawPass(context,currentCommands.First().CreateConfig(context), currentHandlers.ToArray()));
                
                currentCommands.Clear();
                currentHandlers.Clear();
                //passes.Add(new ViewsDrawPass(currentCommands.First().CreateConfig(context),currentHandlers.ToArray()));
                //passes.Add(currentCommands.First().CreatePass(new PassCreateInfo(context, currentCommands.ToArray())));
                continue;
            }

            if (currentCommands.Count > 0)
            {
                var firstCmd = currentCommands.First();
                if (currentHandlerType != cmd.HandlerType)
                {
                    currentHandlers.Add(firstCmd.CreateHandler(currentCommands.ToArray()));
                    currentCommands.Clear();
                }

                if (currentPassConfigType != cmd.PassConfigType)
                {
                    passes.Add(new ViewsDrawPass(context,firstCmd.CreateConfig(context),
                        currentHandlers.ToArray()));
                    currentHandlers.Clear();
                }
            }

            currentCommands.Add(cmd);
            currentHandlerType = cmd.HandlerType;
            currentPassConfigType = cmd.PassConfigType;
        }

        if (currentCommands.NotEmpty())
        {
            currentHandlers.Add(currentCommands.First().CreateHandler(currentCommands.ToArray()));
            passes.Add(new ViewsDrawPass(context,currentCommands.First().CreateConfig(context), currentHandlers.ToArray()));
        }
    }

    public SurfaceContext SurfaceContext { get; }
}