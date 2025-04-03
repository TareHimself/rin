using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using Utils = Rin.Engine.Utils;

namespace Rin.Engine.Views.Graphics;

public sealed class ViewsPass : IPass
{
    private readonly Vector2<uint> _drawSize;
    private readonly List<Pair<ulong, ulong>> _offsets = [];
    private readonly PassInfo _passInfo;
    private readonly Surface _surface;
    public readonly Vector2 SurfaceSize;

    private uint _bufferResourceHandle;
    private ulong _memoryNeeded;
    private FrameStats _stats;

    public ViewsPass(Surface surface, Vector2 drawSize, PassInfo passInfo)
    {
        _surface = surface;
        _passInfo = passInfo;
        SurfaceSize = drawSize;
        _drawSize = SurfaceSize.Mutate(c => new Vector2<uint>((uint)c.X, (uint)c.Y));
        _stats = surface.Stats;
    }

    [PublicAPI] public uint MainImageId { get; set; }

    [PublicAPI] public uint CopyImageId { get; set; }

    [PublicAPI] public uint StencilImageId { get; set; }

    public string Name => "View Pass";

    [PublicAPI] public bool IsTerminal { get; set; }

    public void Added(IGraphBuilder builder)
    {
        foreach (var cmd in _passInfo.PreCommands) cmd.BeforeAdd(builder);

        foreach (var cmd in _passInfo.PostCommands) cmd.BeforeAdd(builder);
    }

    public void Configure(IGraphConfig config)
    {
        foreach (var cmd in _passInfo.PreCommands) cmd.Configure(config);

        MainImageId = config.CreateImage(_drawSize.X, _drawSize.Y, ImageFormat.RGBA32);
        CopyImageId = config.CreateImage(_drawSize.X, _drawSize.Y, ImageFormat.RGBA32);
        StencilImageId = config.CreateImage(_drawSize.X, _drawSize.Y, ImageFormat.Stencil);

        foreach (var drawCommand in _passInfo.Commands)
            if (drawCommand.Type == CommandType.BatchedDraw)
            {
                var offset = _memoryNeeded;

                foreach (var size in drawCommand.Batch!.GetMemoryNeeded())
                    // memoryNeeded += size;
                    // var dist = memoryNeeded & minOffsetAlignment;
                    // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
                    _memoryNeeded += size;
                _offsets.Add(new Pair<ulong, ulong>(offset, _memoryNeeded - offset));
            }
            else if (drawCommand.Type == CommandType.ClipDraw)
            {
                var offset = _memoryNeeded;
                _memoryNeeded += Utils.ByteSizeOf<StencilClip>(drawCommand.Clips.Length);
                _offsets.Add(new Pair<ulong, ulong>(offset, _memoryNeeded - offset));
                // var dist = memoryNeeded & minOffsetAlignment;
                // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
            }
            else if (drawCommand is { Type: CommandType.Custom, Custom: not null })
            {
                var memoryNeeded = drawCommand.Custom.GetRequiredMemory();

                if (memoryNeeded == 0) continue;

                var offset = _memoryNeeded;

                _memoryNeeded += memoryNeeded;

                _offsets.Add(new Pair<ulong, ulong>(offset, _memoryNeeded - offset));
            }


        if (_memoryNeeded > 0) _bufferResourceHandle = config.AllocateBuffer(_memoryNeeded);

        foreach (var cmd in _passInfo.PostCommands) cmd.Configure(config);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var drawImage = graph.GetImage(MainImageId);
        var copyImage = graph.GetImage(CopyImageId);
        var stencilImage = graph.GetImage(StencilImageId);
        var buffer = _bufferResourceHandle > 0 ? graph.GetBuffer(_bufferResourceHandle) : null;
        var cmd = frame.GetCommandBuffer();
        var isWritingStencil = false;
        var isComparingStencil = false;

        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

        _stats.FinalCommandCount = _passInfo.Commands.Count;
        _stats.MemoryAllocatedBytes = _memoryNeeded;
        var offsetsAndSizes = _offsets.GetEnumerator();
        offsetsAndSizes.MoveNext();

        ResetStencilState(cmd);
        cmd
            .ImageBarrier(drawImage, ImageLayout.General)
            .ImageBarrier(copyImage, ImageLayout.General)
            .ImageBarrier(stencilImage, ImageLayout.General)
            .ClearColorImages(new Vector4(0.0f), ImageLayout.General, drawImage, copyImage)
            .ClearStencilImages(0, stencilImage.Layout, stencilImage)
            .ImageBarrier(drawImage, ImageLayout.ColorAttachment)
            .ImageBarrier(copyImage, ImageLayout.ShaderReadOnly)
            .ImageBarrier(stencilImage, ImageLayout.General, ImageLayout.StencilAttachment);

        var viewFrame = new ViewsFrame(_surface, frame, SurfaceSize, drawImage, copyImage, stencilImage, _stats);

        foreach (var command in _passInfo.PreCommands) command.Execute(viewFrame);

        foreach (var command in _passInfo.Commands)
            switch (command.Type)
            {
                case CommandType.None:
                    break;
                case CommandType.ClipDraw:
                {
                    _surface.BeginMainPass(viewFrame);

                    if (!isWritingStencil)
                    {
                        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkCompareOp.VK_COMPARE_OP_ALWAYS);
                        cmd.SetWriteMask(0, 1, 0);
                        if (SViewsModule.Get().GetStencilShader() is { } stencilShader &&
                            stencilShader.Bind(cmd, true))
                        {
                            isWritingStencil = true;
                            isComparingStencil = false;
                        }
                    }

                    {
                        if (isWritingStencil && buffer != null && SViewsModule.Get().GetStencilShader() is
                                { } stencilShader)
                        {
                            vkCmdSetStencilWriteMask(cmd, faceFlags, command.Mask);

                            var (offset, size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            view.Write(command.Clips, offset);

                            var push = new StencilPushConstant
                            {
                                Projection = viewFrame.Projection,
                                data = view.GetAddress()
                            };
                            var pushResource = stencilShader.PushConstants.First().Value;
                            cmd.PushConstant(stencilShader.GetPipelineLayout(), pushResource.Stages, push);

                            vkCmdDraw(cmd, 6, (uint)command.Clips.Length, 0, 0);
                        }
                    }
                }
                    break;
                case CommandType.ClipClear:
                {
                    if (viewFrame.ActivePass.Length == 0) _surface.BeginMainPass(viewFrame);

                    var clearAttachment = new VkClearAttachment
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
                        clearValue = new VkClearValue
                        {
                            color = SGraphicsModule.MakeClearColorValue(new Vector4(0.0f)),
                            depthStencil = new VkClearDepthStencilValue
                            {
                                stencil = 0
                            }
                        }
                    };
                    unsafe
                    {
                        var extent = stencilImage.Extent;
                        var clearRect = new VkClearRect
                        {
                            baseArrayLayer = 0,
                            layerCount = 1,
                            rect = new VkRect2D
                            {
                                offset = new VkOffset2D
                                {
                                    x = 0,
                                    y = 0
                                },
                                extent = new VkExtent2D
                                {
                                    width = extent.Width,
                                    height = extent.Height
                                }
                            }
                        };

                        vkCmdClearAttachments(cmd, 1, &clearAttachment, 1, &clearRect);
                    }
                }
                    break;
                case CommandType.BatchedDraw:
                case CommandType.Custom:
                {
                    //BeginMainPass(viewFrame);
                    if (!isComparingStencil)
                    {
                        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);
                        cmd.SetWriteMask(0, 1,
                            VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                            VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);
                        isWritingStencil = false;
                        isComparingStencil = true;
                    }

                    vkCmdSetStencilCompareMask(cmd, faceFlags, command.Mask);

                    switch (command)
                    {
                        case { Type: CommandType.Custom, Custom: not null }:
                        {
                            var willUseMemory = command.Custom.GetRequiredMemory() > 0;
                            if (willUseMemory && buffer != null)
                            {
                                var (offset, size) = offsetsAndSizes.Current;
                                offsetsAndSizes.MoveNext();
                                var view = buffer.GetView(offset, size);
                                command.Custom.Run(viewFrame, command.Mask, view);
                            }
                            else
                            {
                                command.Custom.Run(viewFrame, command.Mask);
                            }
                        }
                            break;
                        case { Type: CommandType.BatchedDraw, Batch: not null } when buffer != null:
                        {
                            var batch = command.Batch!;
                            var (offset, size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            batch.GetRenderer().Draw(viewFrame, batch, view);
                            break;
                        }
                    }
                }
                    break;
            }

        if (viewFrame.IsAnyPassActive) viewFrame.EndActivePass();

        foreach (var command in _passInfo.PostCommands) command.Execute(viewFrame);
    }

    public uint Id { get; set; }

    public void Dispose()
    {
    }


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