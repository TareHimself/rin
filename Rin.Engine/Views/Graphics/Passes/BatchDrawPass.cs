using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

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
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        // foreach (var cmd in _passInfo.PreCommands) cmd.BeforeAdd(builder);
        //
        // foreach (var cmd in _passInfo.PostCommands) cmd.BeforeAdd(builder);
    }

    public void Configure(IGraphConfig config)
    {
        MainImageId = config.WriteImage(Context.MainImageId, ImageLayout.ColorAttachment);
        CopyImageId = config.ReadImage(Context.CopyImageId, ImageLayout.ShaderReadOnly);
        StencilImageId = config.ReadImage(Context.StencilImageId, ImageLayout.StencilAttachment);

        var memoryNeeded = _batchSizes.Aggregate<ulong, ulong>(0, (t, c) => t + c);

        if (memoryNeeded > 0) _bufferId = config.CreateBuffer(memoryNeeded, BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var drawImage = graph.GetImage(MainImageId);
        var copyImage = graph.GetImage(CopyImageId);
        var stencilImage = graph.GetImage(StencilImageId);
        var buffer = graph.GetBufferOrNull(_bufferId);
        var cmd = frame.GetCommandBuffer();

        var viewFrame = new ViewsFrame(frame, drawImage, copyImage, stencilImage, Context);

        // foreach (var command in _passInfo.PreCommands) command.Execute(viewFrame);
        cmd.BeginRendering(_extent.ToVk(), [
                drawImage.MakeColorAttachmentInfo()
            ],
            stencilAttachment: stencilImage.MakeStencilAttachmentInfo()
        );

        frame.ConfigureForViews(_extent);
        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);

        cmd.SetWriteMask(0, 1,
            VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
            VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
            VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
            VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);

        var compareMask = uint.MaxValue;

        ulong offset = 0;
        for (var i = 0; i < _batches.Count; i++)
        {
            var (batch, currentCompareMask) = _batches[i];
            if (currentCompareMask != compareMask)
            {
                compareMask = currentCompareMask;
                vkCmdSetStencilCompareMask(cmd, faceFlags, compareMask);
            }

            var batcher = batch.GetBatcher();
            var bufferSize = _batchSizes[i];
            var view = bufferSize > 0 ? buffer?.GetView(offset, bufferSize) : null;
            batcher.Draw(viewFrame, batch, view);
            offset += bufferSize;
        }

        cmd.EndRendering();
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new BatchDrawPass(info);
    }

    public uint Id { get; set; }

    private static void ResetStencilState(VkCommandBuffer cmd,
        VkStencilFaceFlags faceMask = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK)
    {
        vkCmdSetStencilTestEnable(cmd, 1);
        vkCmdSetStencilReference(cmd, faceMask, 255);
        vkCmdSetStencilWriteMask(cmd, faceMask, 0x01);
        vkCmdSetStencilCompareMask(cmd, faceMask, 0x01);
        vkCmdSetStencilOp(cmd, faceMask, VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkCompareOp.VK_COMPARE_OP_NEVER);
    }
}