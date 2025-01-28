using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using Utils = rin.Framework.Core.Utils;

namespace rin.Framework.Views.Graphics;

public sealed class ViewsPass(Surface surface,PassInfo passInfo) : IPass
{
    private uint _drawImageHandle;
    private uint _copyImageHandle;
    private uint _stencilImageHandle;
    private uint _bufferResourceHandle;
    private readonly Vec2<uint> _drawSize = surface.GetDrawSize().Cast<uint>();
    private ulong _memoryNeeded = 0;
    private readonly List<Pair<ulong, ulong>> _offsets = [];
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void BeforeAdd(IGraphBuilder builder)
    {
        foreach (var cmd in passInfo.PreCommands)
        {
            cmd.BeforeAdd(builder);
        }
        
        foreach (var cmd in passInfo.PostCommands)
        {
            cmd.BeforeAdd(builder);
        }
    }

    public void Configure(IGraphConfig config)
    {
        foreach (var cmd in passInfo.PreCommands)
        {
            cmd.Configure(config);
        }
        
        foreach (var cmd in passInfo.PostCommands)
        {
            cmd.Configure(config);
        }
        
        _drawImageHandle = config.CreateImage(_drawSize.X, _drawSize.Y, ImageFormat.RGBA32);
        _copyImageHandle = config.CreateImage(_drawSize.X, _drawSize.Y, ImageFormat.RGBA32);
        _stencilImageHandle = config.CreateImage( _drawSize.X, _drawSize.Y, ImageFormat.Stencil);
        
        foreach (var drawCommand in passInfo.Commands)
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
                _memoryNeeded += Utils.ByteSizeOf<StencilClip>(drawCommand.Clips.Length);
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
            _bufferResourceHandle = config.AllocateBuffer(_memoryNeeded);
        }
    }

    public void Execute(ICompiledGraph graph, Frame frame, VkCommandBuffer cmd)
    {
        var drawImage = graph.GetImage(_drawImageHandle!).AsImage();
        var copyImage = graph.GetImage(_copyImageHandle!).AsImage();
        var stencilImage = graph.GetImage(_stencilImageHandle!).AsImage();
        var buffer = _bufferResourceHandle > 0  ? graph.GetBuffer(_bufferResourceHandle) : null;
        var viewFrame = new ViewsFrame(surface, frame,drawImage, copyImage, stencilImage);
        
        var isWritingStencil = false;
        var isComparingStencil = false;

        var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;
        
        surface.Stats.FinalCommandCount = passInfo.Commands.Count;
        surface.Stats.MemoryAllocatedBytes = _memoryNeeded;
        var offsetsAndSizes = _offsets.GetEnumerator();
        offsetsAndSizes.MoveNext();
        
        BeforeDraw(cmd,drawImage, copyImage, stencilImage);
        
        foreach (var command in passInfo.PreCommands)
        {
            command.Execute(viewFrame);
        }
        
        foreach (var command in passInfo.Commands)
        {
            switch (command.Type)
            {
                case CommandType.None:
                    break;
                case CommandType.ClipDraw:
                {
                    surface.BeginMainPass(viewFrame);

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
                            
                            var (offset,size) = offsetsAndSizes.Current;
                            offsetsAndSizes.MoveNext();
                            var view = buffer.GetView(offset, size);
                            view.Write(command.Clips, (ulong)offset);

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
                    if (viewFrame.ActivePass.Length == 0) surface.BeginMainPass(viewFrame);

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
                            var willUseMemory = command.Custom.MemoryNeeded > 0;
                            if (willUseMemory && buffer != null)
                            {
                                var (offset,size) = offsetsAndSizes.Current;
                                offsetsAndSizes.MoveNext();
                                var view = buffer.GetView(offset, size);
                                command.Custom.Run(viewFrame,command.Mask,view);
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
                            batch.GetRenderer().Draw(viewFrame, batch,view);
                            break;
                        }
                    }
                }
                    break;
            }
        }

        if (viewFrame.IsAnyPassActive)
        {
            viewFrame.EndActivePass();
        }
        
        foreach (var command in passInfo.PostCommands)
        {
            command.Execute(viewFrame);
        }

        cmd.ImageBarrier(drawImage, ImageLayout.ColorAttachment, ImageLayout.TransferSrc);

        frame.OnCopy += (_, image, extent) =>
        {
            cmd.CopyImageToImage(drawImage, image,new VkExtent3D()
            {
                width = extent.width,
                height = extent.height,
                depth = 1
            });
        };
    }

    public uint Id { get; set; }

    public void BeforeDraw(VkCommandBuffer cmd,IDeviceImage drawImage,IDeviceImage copyImage,IDeviceImage stencilImage)
    {
        ResetStencilState(cmd);
        cmd.ImageBarrier(drawImage, ImageLayout.Undefined, ImageLayout.General);
        cmd.ImageBarrier(copyImage, ImageLayout.Undefined, ImageLayout.General);
        cmd.ImageBarrier(stencilImage, ImageLayout.Undefined,
            ImageLayout.General, new ImageBarrierOptions
            {
                SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                    VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
            });
        cmd.ClearColorImages(0.0f, ImageLayout.General, drawImage, copyImage);
        cmd.ClearStencilImages(0, ImageLayout.General, stencilImage);
        cmd.ImageBarrier(drawImage,  ImageLayout.General,ImageLayout.ColorAttachment);
        cmd.ImageBarrier(copyImage, ImageLayout.General,ImageLayout.ShaderReadOnly);
        cmd.ImageBarrier(stencilImage, ImageLayout.General,
            ImageLayout.StencilAttachment, new ImageBarrierOptions
            {
                SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                    VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
            });
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

    public string Name => "View Pass";
    public bool IsTerminal => true;
}