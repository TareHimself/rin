using System.Collections.Frozen;
using System.Diagnostics;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Descriptors;

namespace Rin.Framework.Graphics.Vulkan.Shaders;

public abstract class VulkanBindContext<TInterface> : IBindContext<TInterface> where TInterface : IBindContext
{
    private readonly Dictionary<uint, DescriptorSet> _descriptorSets = [];
    private readonly HashSet<DescriptorSet> _setsPendingUpdate = [];
    protected readonly VulkanExecutionContext ExecutionContext;

    public VulkanBindContext(VulkanExecutionContext executionContext)
    {
        ExecutionContext = executionContext;
    }

    protected abstract FrozenDictionary<string, Resource> Resources { get; }
    protected abstract FrozenDictionary<string, PushConstant> PushConstants { get; }

    /// <summary>
    /// Must be called every set dependent operation to ensure we update the sets before draws
    /// </summary>
    protected void UpdatePendingSets()
    {
        if(_setsPendingUpdate.Count == 0) return;
        
        foreach (var set in _setsPendingUpdate)
        {
            set.Update();
        }
        _setsPendingUpdate.Clear();
    }
    public TInterface Reset()
    {
        _descriptorSets.Clear();
        return GetInterface();
    }

    public abstract TInterface Push<T>(in T data, uint offset = 0) where T : unmanaged;

    public TInterface WriteBuffer(string name, in DeviceBufferView view, uint arrayOffset = 0)
    {
        Debug.Assert(Resources.ContainsKey(name), $"{name} is not a resource");

        var resource = Resources[name];

        Debug.Assert(resource.Type is DescriptorType.StorageBuffer or DescriptorType.UniformBuffer,
            $"{name} is not a buffer");

        var descriptorSet = GetDescriptorSet(resource);

        switch (resource.Type)
        {
            case DescriptorType.StorageBuffer:
                descriptorSet.WriteStorageBuffer(resource.Binding, view, arrayOffset);
                break;
            case DescriptorType.UniformBuffer:
                descriptorSet.WriteUniformBuffer(resource.Binding, view, arrayOffset);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _setsPendingUpdate.Add(descriptorSet);

        return GetInterface();
    }
    
    protected abstract IShader GetShader();
    protected abstract TInterface GetInterface();

    private DescriptorSet GetDescriptorSet(Resource resource)
    {
        var shader = GetShader();
        var setIndex = resource.Set;
        var set = _descriptorSets.GetValueOrDefault(setIndex);

        if (set is not null) return set;

        set = ExecutionContext.FindGlobalDescriptorSet(setIndex);

        if (set is not null) return _descriptorSets[setIndex] = set;

        set = ExecutionContext.AllocateDescriptorSet(shader, setIndex);

        // We just created the set so we need to bind it
        ExecutionContext.BindDescriptorSets(shader, setIndex, set);

        return _descriptorSets[setIndex] = set;
    }
}