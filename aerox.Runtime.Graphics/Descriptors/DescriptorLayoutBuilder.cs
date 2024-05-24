using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics.Descriptors;

public class DescriptorLayoutBuilder
{
    private readonly Dictionary<uint, VkDescriptorSetLayoutBinding> _bindings = new();

    public DescriptorLayoutBuilder AddBinding(uint binding, VkDescriptorType type, VkShaderStageFlags stages,
        uint count = 1)
    {
        if (_bindings.TryGetValue(binding, out var existing))
        {
            existing.stageFlags |= stages;
            _bindings[binding] = existing;
            return this;
        }


        var newBinding = new VkDescriptorSetLayoutBinding
        {
            stageFlags = stages,
            descriptorType = type,
            descriptorCount = count,
            binding = binding
        };
        _bindings.Add(binding, newBinding);
        return this;
    }

    public DescriptorLayoutBuilder Clear()
    {
        _bindings.Clear();
        return this;
    }

    public unsafe VkDescriptorSetLayout Build()
    {
        var device = Runtime.Instance.GetModule<GraphicsModule>().GetDevice();
        var bindings = new List<VkDescriptorSetLayoutBinding>();

        var flags = new List<VkDescriptorBindingFlags>();

        foreach (var kv in _bindings)
        {
            bindings.Add(kv.Value);
            flags.Add(VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT);
        }

        VkDescriptorSetLayoutBindingFlagsCreateInfo pNext = default;
        pNext.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_BINDING_FLAGS_CREATE_INFO;

        fixed (VkDescriptorBindingFlags* pFlags = flags.ToArray())
        {
            pNext.pBindingFlags = pFlags;
            pNext.bindingCount = (uint)flags.Count;
        }

        VkDescriptorSetLayoutCreateInfo createInfo = default;
        createInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
        createInfo.flags = VkDescriptorSetLayoutCreateFlags.VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT;
        fixed (VkDescriptorSetLayoutBinding* pBindings = bindings.ToArray())
        {
            createInfo.pBindings = pBindings;
            createInfo.bindingCount = (uint)bindings.Count;
        }

        createInfo.pNext = &pNext;

        VkDescriptorSetLayout layout;
        vkCreateDescriptorSetLayout(device, &createInfo, null, &layout);
        return layout;
    }
}