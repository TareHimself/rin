using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics.Descriptors;

public class DescriptorLayoutBuilder
{
    private readonly Dictionary<uint, VkDescriptorSetLayoutBinding> _bindings = [];
    private readonly Dictionary<uint, VkDescriptorBindingFlags> _flags = [];

    public DescriptorLayoutBuilder AddBinding(uint binding, VkDescriptorType type, VkShaderStageFlags stages,
        uint count = 1,VkDescriptorBindingFlags bindingFlags = 0)
    {
        if (_bindings.TryGetValue(binding, out var existing))
        {
            existing.stageFlags |= stages;
            _flags[binding] |= bindingFlags;
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
        _flags.Add(binding,bindingFlags);
        return this;
    }

    public DescriptorLayoutBuilder Clear()
    {
        _bindings.Clear();
        _flags.Clear();
        return this;
    }

    public unsafe VkDescriptorSetLayout Build()
    {
        var device = SRuntime.Get().GetModule<SGraphicsModule>().GetDevice();
        var bindings = new List<VkDescriptorSetLayoutBinding>();

        var allFlags = new List<VkDescriptorBindingFlags>();

        VkDescriptorSetLayoutCreateFlags setLayoutCreateFlags = 0;
        foreach (var (binding,info) in _bindings)
        {
            var flags = _flags[binding];
            bindings.Add(info);
            allFlags.Add(flags);
            if ((flags & VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT) != (VkDescriptorBindingFlags)0)
            {
                setLayoutCreateFlags |= VkDescriptorSetLayoutCreateFlags
                    .VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT;
            }
        }

        VkDescriptorSetLayoutBindingFlagsCreateInfo pNext = default;
        pNext.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_BINDING_FLAGS_CREATE_INFO;

        fixed (VkDescriptorBindingFlags* pFlags = allFlags.ToArray())
        {
            pNext.pBindingFlags = pFlags;
            pNext.bindingCount = (uint)allFlags.Count;
        }

        VkDescriptorSetLayoutCreateInfo createInfo = default;
        createInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
        createInfo.flags = setLayoutCreateFlags;
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