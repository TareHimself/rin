using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.Graphics.FrameGraph;

public class ExecutionContext(CompiledGraph graph,Frame frame) : IExecutionContext, IDisposable
{
    private readonly VkCommandBuffer _primaryCommandBuffer = frame.GetPrimaryCommandBuffer();
    //private bool _primaryAvailable = true;
    private readonly Lock _lock = new Lock();
    private readonly List<VkCommandBuffer> _executedCommandBuffers = [];
    private readonly HashSet<VkCommandBuffer> _activeSecondaries = [];
    public void ExecuteSecondaries()
    {
        lock (_lock)
        {
            if (_activeSecondaries.Count > 0)
            {
                var buffers = _activeSecondaries.ToArray();
                _activeSecondaries.Clear();
                _executedCommandBuffers.AddRange(buffers);
                foreach (var cmd in buffers)
                {
                    cmd.End();
                }

                unsafe
                {
                    fixed (VkCommandBuffer* pBuffers = buffers)
                    {
                        vkCmdExecuteCommands(_primaryCommandBuffer,(uint)buffers.Length,pBuffers);
                    }
                }
            
            }
        }
    }
    
    public VkCommandBuffer NewCmd()
    {
        lock (_lock)
        {
            // if (_primaryAvailable)
            // {
            //     _primaryAvailable = false;
            //     return _primaryCommandBuffer;
            // }
            //
            if (_activeSecondaries.Count > 0)
            {
                var cmd = _activeSecondaries.First();
                _activeSecondaries.Remove(cmd);
                return cmd;
            }

            {
                var cmd = frame.AllocateSecondaryCommandBuffer();
                cmd.BeginSecondary();
                return cmd;
            }
        }
    }
    
    public void FreeCmd(in VkCommandBuffer cmd)
    {
        lock (_lock)
        {
            _activeSecondaries.Add(cmd);
        }
    }

    public ICommandBufferContext UsingCmd()
    {
        return new CommandBufferContext(this,NewCmd());
    }

    public DescriptorAllocator DescriptorAllocator { get; } = frame.GetDescriptorAllocator();

    public void Dispose()
    {
        lock (_lock)
        {
            frame.FreeCommandBuffers(_executedCommandBuffers);
        }
    }
}