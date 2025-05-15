using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class CompiledGraph : ICompiledGraph
{
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly Frame _frame;
    private readonly Dictionary<uint, IGraphImage> _images = [];
    private readonly IEnumerable<ExecutionGroup> _nodes;
    private readonly IResourcePool _resourcePool;

#if DEBUG
    private readonly Dictionary<uint, IDeviceBuffer> _buffers = [];
#else
    private readonly IDeviceBuffer? _buffer;
    private ulong _bufferOffset;
    private readonly Dictionary<uint, IDeviceBufferView> _buffers = [];
#endif

    public CompiledGraph(IResourcePool resourcePool, Frame frame, Dictionary<uint, IResourceDescriptor> descriptors,
        IEnumerable<ExecutionGroup> nodes)
    {
        _resourcePool = resourcePool;
        _frame = frame;

#if !DEBUG
        var memoryNeeded = descriptors.Values.Aggregate((ulong)0, (total, descriptor) =>
        {
            if (descriptor is BufferResourceDescriptor asMemoryDescriptor) return total + asMemoryDescriptor.Size;

            return total;
        });

        if (SEngine.Get().IsModuleLoaded<SGraphicsModule>() && memoryNeeded > 0)
        {
            var pooledView = resourcePool.CreateBuffer(new BufferResourceDescriptor(memoryNeeded), frame);
            //pooledView = new DeviceBufferWriteValidator(pooledView);
            _buffer =
                pooledView; // SGraphicsModule.Get().NewStorageBuffer(memoryNeeded,debugName: "Compiled Frame Graph Memory");
        }
#endif

        _descriptors = descriptors;

        _nodes = nodes;
    }

    public void Dispose()
    {
        foreach (var image in _images.Values) image.Dispose();

        //if(resource is not DeviceImage) resource.Dispose();
        _images.Clear();

#if DEBUG
        foreach (var buffers in _buffers.Values)
        {
            buffers.Dispose();
        }

        _buffers.Clear();
#else
        _buffers.Clear();
        _buffer?.Dispose();
#endif
    }


    public IGraphImage GetImage(uint id)
    {
        {
            if (_images.TryGetValue(id, out var resource)) return resource;
        }

        {
            if (_descriptors.TryGetValue(id, out var descriptor))
            {
                {
                    if (descriptor is ImageResourceDescriptor asImageResourceDescriptor)
                    {
                        var image = _resourcePool.CreateImage(asImageResourceDescriptor, _frame);
                        _images.Add(id, image);
                        return image;
                    }
                }

                {
                    if (descriptor is ExternalImageResourceDescriptor asExternalImageResourceDescriptor)
                    {
                        var image = asExternalImageResourceDescriptor.Image;
                        _images.Add(id, image);
                        return image;
                    }
                }
            }
        }

        throw new ResourceAllocationException(id);
    }

    public IDeviceBufferView GetBuffer(uint id)
    {
#if DEBUG
        {
            if (_buffers.TryGetValue(id, out var resource)) return resource.GetView();
        }
        if (_descriptors.TryGetValue(id, out var descriptor) &&
            descriptor is BufferResourceDescriptor asMemoryDescriptor)
        {
            var buffer = _resourcePool.CreateBuffer(asMemoryDescriptor, _frame);
            _buffers.Add(id, buffer);
            return buffer.GetView();
        }

#else
        {
            if (_buffers.TryGetValue(id, out var resource)) return resource;
        }
        if (_buffer != null && _descriptors.TryGetValue(id, out var descriptor) &&
            descriptor is BufferResourceDescriptor asMemoryDescriptor)
        {
            var view = _buffer.GetView(_bufferOffset, asMemoryDescriptor.Size);
            _bufferOffset += view.Size;
            _buffers.Add(id, view);
            return view;
        }
#endif

        throw new ResourceAllocationException(id);
    }

    public void Execute(Frame frame, IRenderContext context, TaskPool taskPool)
    {
        var cmd = frame.GetPrimaryCommandBuffer();
        foreach (var stage in _nodes)
        foreach (var pass in stage.Passes)
            pass.Execute(this,cmd, frame, context);
        // if (stage.IsBarrier)
        // {
        //     if (used.NotEmpty())
        //     {
        //         unsafe
        //         {
        //             foreach (var cmd in used)
        //             {
        //                 cmd.End();
        //             }
        //         
        //             fixed (VkCommandBuffer* pCmds = used.ToArray())
        //             {
        //                 vkCmdExecuteCommands(primaryCmd,(uint)used.Count,pCmds);
        //             }
        //
        //             foreach (var cmd in used)
        //             {
        //                 cmd.BeginSecondary();
        //             }
        //         }
        //     }
        //     
        //     foreach (var pass in stage.Passes)
        //     {
        //         pass.Execute(this,primaryCmd,frame,context);
        //     }
        //     
        //     used.Clear();
        // }
        // else
        // {
        //     var tasks = stage.Passes.Select(node => taskPool.Enqueue(() =>
        //     {
        //         var cmd = cmds.Pop();
        //         node.Execute(this, cmd, frame, context);
        //         used.Add(cmd);
        //         cmds.Push(cmd);
        //     })).ToArray();
        //     
        //     foreach (var task in tasks)
        //     {
        //         task.Wait();
        //     }
        // }
        // var tasks = stage.Select(node => taskPool.Enqueue(() =>
        // {
        //     foreach (var cmd in cmds.GetConsumingEnumerable())
        //     {
        //         node.Execute(this, cmd, frame, context);
        //         cmds.Add(cmd);
        //         break;
        //     }
        // })).ToArray();
        //
        // foreach (var task in tasks)
        // {
        //     task.Wait();
        // }
    }
}