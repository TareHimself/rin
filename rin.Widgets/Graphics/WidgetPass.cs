using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Math;
using rin.Graphics;
using rin.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace rin.Widgets.Graphics;

public class WidgetPass(Surface surface,FinalDrawCommand[] commands) : IPass
{
    private IResourceHandle? _drawImageHandle;
    private IResourceHandle? _copyImageHandle;
    private IResourceHandle? _stencilImageHandle;
    private IResourceHandle? _bufferResourceHandle;
    private readonly Vector2<uint> _drawSize = surface.GetDrawSize().Cast<uint>();
    private ulong _memoryNeeded = 0;
    private readonly List<Pair<ulong, ulong>> _offsets = [];
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphBuilder builder)
    {
        _drawImageHandle = builder.RequestImage(this, _drawSize.X, _drawSize.Y, ImageFormat.Rgba32,
            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
        _copyImageHandle = builder.RequestImage(this, _drawSize.X, _drawSize.Y, ImageFormat.Rgba32,
            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
        _stencilImageHandle = builder.RequestImage(this, _drawSize.X, _drawSize.Y, ImageFormat.Stencil,
            VkImageLayout.VK_IMAGE_LAYOUT_GENERAL);
        
        foreach (var drawCommand in commands)
            if (drawCommand.Type == CommandType.BatchedDraw)
            {
                var offset = _memoryNeeded;
                
                foreach (var size in drawCommand.Batch!.GetMemoryNeeded())
                {
                    // memoryNeeded += size;
                    // var dist = memoryNeeded & minOffsetAlignment;
                    // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
                    _memoryNeeded += size;
                }
                _offsets.Add(new Pair<ulong, ulong>(offset,_memoryNeeded - offset));
            }
            else if (drawCommand.Type == CommandType.ClipDraw)
            {
                var offset = _memoryNeeded;
                _memoryNeeded += (ulong)(Marshal.SizeOf<StencilClip>() * drawCommand.Clips.Length);
                _offsets.Add(new Pair<ulong, ulong>(offset,_memoryNeeded - offset));
                // var dist = memoryNeeded & minOffsetAlignment;
                // memoryNeeded += dist > 0 ? minOffsetAlignment - dist : 0;
            }
            else if (drawCommand is { Type: CommandType.Custom, Custom.MemoryNeeded: {} asCustomMemory } && asCustomMemory != 0)
            {
                var offset = (_memoryNeeded);
                _memoryNeeded += asCustomMemory;
                _offsets.Add(new Pair<ulong, ulong>(offset,_memoryNeeded - offset));
            }


        if (_memoryNeeded > 0)
        {
            _bufferResourceHandle = builder.RequestMemory(this, _memoryNeeded);
        }
    }

    public void Execute(ICompiledGraph graph, Frame frame, VkCommandBuffer cmd)
    {
        var drawImage = graph.GetResource(_drawImageHandle!).AsImage();
        var copyImage = graph.GetResource(_copyImageHandle!).AsImage();
        var stencilImage = graph.GetResource(_stencilImageHandle!).AsImage();
        var buffer = _bufferResourceHandle is { } asResourceHandle ? graph.GetResource(asResourceHandle).AsMemory() : null;
        var widgetFrame = new WidgetFrame(surface, frame,drawImage, copyImage, stencilImage);
        
        var isWritingStencil = false;
        var isComparingStencil = false;

        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
        
        surface.Stats.FinalCommandCount = commands.Length;
        surface.Stats.MemoryAllocatedBytes = _memoryNeeded;
        
        if (buffer != null) frame.OnReset += _ => buffer.Dispose();
        var offsetsAndSizes = _offsets.GetEnumerator();
        offsetsAndSizes.MoveNext();
        
        BeforeDraw(cmd,drawImage, copyImage, stencilImage);
        
        for (var i = 0; i < commands.Length; i++)
        {
            var command = commands[i];

            switch (command.Type)
            {
                case CommandType.None:
                    break;
                case CommandType.ClipDraw:
                {
                    surface.BeginMainPass(widgetFrame);

                    if (!isWritingStencil)
                    {
                        vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkStencilOp.VK_STENCIL_OP_REPLACE, VkStencilOp.VK_STENCIL_OP_KEEP,
                            VkCompareOp.VK_COMPARE_OP_ALWAYS);
                        cmd.SetWriteMask(0, 1, 0);
                        if (SWidgetsModule.Get().GetStencilShader() is { } stencilShader &&
                            stencilShader.Bind(cmd, true))
                        {
                            isWritingStencil = true;
                            isComparingStencil = false;
                        }
                    }

                    {
                        if (isWritingStencil && buffer != null && SWidgetsModule.Get().GetStencilShader() is
                                { } stencilShader)
                        {
                            vkCmdSetStencilWriteMask(cmd, faceFlags, command.Mask);
                            
                            var (offset,size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            frame.OnReset += (_) => view.Dispose();
                            
                            view.Write(command.Clips, offset);

                            var push = new StencilPushConstant
                            {
                                Projection = widgetFrame.Projection,
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
                    if (widgetFrame.ActivePass.Length == 0) surface.BeginMainPass(widgetFrame);

                    var clearAttachment = new VkClearAttachment
                    {
                        aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT,
                        clearValue = new VkClearValue
                        {
                            color = SGraphicsModule.MakeClearColorValue(0.0f),
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
                                    width = extent.width,
                                    height = extent.height
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
                    //BeginMainPass(widgetFrame);

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
                            var willUseMemory = command.Custom.MemoryNeeded > 0;
                            if (willUseMemory && buffer != null)
                            {
                                var (offset,size) = offsetsAndSizes.Current;
                                offsetsAndSizes.MoveNext();
                                var view = buffer.GetView(offset, size);
                                frame.OnReset += (_) => view.Dispose();
                                command.Custom.Run(widgetFrame,command.Mask,view);
                            }
                            else
                            {
                                command.Custom.Run(widgetFrame, command.Mask);
                            }
                        }
                            break;
                        case { Type: CommandType.BatchedDraw, Batch: not null } when buffer != null:
                        {
                            var batch = command.Batch!;
                            var (offset, size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            frame.OnReset += (_) => view.Dispose();
                            batch.GetRenderer().Draw(widgetFrame, batch,view);
                            break;
                        }
                    }
                }
                    break;
            }
        }

        if (widgetFrame.IsAnyPassActive)
        {
            widgetFrame.EndActivePass();
        }
        
        
        drawImage.Barrier(cmd,VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);

        frame.OnCopy += (_, image, extent) =>
        {
            drawImage.CopyTo(frame.GetCommandBuffer(),image,new VkExtent3D()
            {
                width = extent.width,
                height = extent.height,
                depth = 1
            });
        };
    }
    
    public virtual void BeforeDraw(VkCommandBuffer cmd,DeviceImage drawImage,DeviceImage copyImage,DeviceImage stencilImage)
    {
        drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL);
        copyImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL);
        unsafe
        {
            fixed (VkClearColorValue* pColor = new[]
                       { SGraphicsModule.MakeClearColorValue(new Vector4<float>(0.0f, 0.0f, 0.0f, 1.0f)) })
            {
                fixed (VkImageSubresourceRange* pRanges = new[]
                           { SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT) })
                {
                    vkCmdClearColorImage(cmd,drawImage.Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, pColor, 1,
                        pRanges);
                    vkCmdClearColorImage(cmd,copyImage.Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, pColor, 1,
                        pRanges);
                }
            }

            drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);
            copyImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
            ResetStencilState(cmd);


            stencilImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, new ImageBarrierOptions
                {
                    SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                        VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                });

            fixed (VkClearDepthStencilValue* pColor = new[]
                       { SGraphicsModule.MakeClearDepthStencilValue(stencil: 0) })
            {
                fixed (VkImageSubresourceRange* pRanges = new[]
                       {
                           SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT |
                                                                     VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                       })
                {
                    vkCmdClearDepthStencilImage(cmd, stencilImage.Image, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                        pColor, 1, pRanges);
                    ResetStencilState(cmd);
                }
            }

            stencilImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_GENERAL,
                VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, new ImageBarrierOptions
                {
                    SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                        VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
                });
        }
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

    public string Name => "Widget Pass";
    public bool IsTerminal => true;
}