using rin.Core;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Graphics.Descriptors;

public struct PoolSizeRatio
{
    public VkDescriptorType type;
    public float ratio;

    public PoolSizeRatio(VkDescriptorType inType, float inRatio)
    {
        type = inType;
        ratio = inRatio;
    }
}

public class DescriptorPool : Disposable
{
    private readonly VkDescriptorPool _descriptorPool;
    private readonly VkDevice _device;

    private readonly List<DescriptorSet> _sets = new();

    public DescriptorPool(VkDevice device, VkDescriptorPool descriptorPool)
    {
        _device = device;
        _descriptorPool = descriptorPool;
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var set in _sets) set.Dispose();
        _sets.Clear();
        unsafe
        {
            vkDestroyDescriptorPool(_device, _descriptorPool, null);
        }
    }

    public unsafe DescriptorSet Allocate(VkDescriptorSetLayout layout,params uint[] variableCount)
    {
        VkDescriptorSetAllocateInfo info = default;
        info.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
        info.pSetLayouts = &layout;
        info.descriptorSetCount = 1;
        info.descriptorPool = _descriptorPool;

        if (variableCount.Length != 0)
        {
            VkDescriptorSetVariableDescriptorCountAllocateInfo variableInfo = default;
            variableInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_VARIABLE_DESCRIPTOR_COUNT_ALLOCATE_INFO;
            variableInfo.descriptorSetCount = (uint)variableCount.Length;
            unsafe
            {
                fixed (uint* pVariableCounts = variableCount)
                {
                    variableInfo.pDescriptorCounts = pVariableCounts;
                    info.pNext = &variableInfo;
                    VkDescriptorSet set;
                    vkAllocateDescriptorSets(_device, &info, &set);
                    var mSet = new DescriptorSet(_device, set);
                    _sets.Add(mSet);
                    return mSet;
                }
            }
        }

        {
            VkDescriptorSet set;
            vkAllocateDescriptorSets(_device, &info, &set);
            var mSet = new DescriptorSet(_device, set);
            _sets.Add(mSet);
            return mSet;
        }
    }

    public void Reset()
    {
        foreach (var set in _sets) set.Dispose();
        _sets.Clear();
        vkResetDescriptorPool(_device, _descriptorPool, 0);
    }
}