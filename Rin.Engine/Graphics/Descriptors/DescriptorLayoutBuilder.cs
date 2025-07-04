using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.VkStructureType;

namespace Rin.Engine.Graphics.Descriptors;

public class DescriptorLayoutBuilder
{
    private readonly Dictionary<uint, VkDescriptorSetLayoutBinding> _bindings = [];
    private readonly Dictionary<uint, VkDescriptorBindingFlags> _flags = [];

    public DescriptorLayoutBuilder AddBinding(uint binding, DescriptorType type, ShaderStage stages,
        uint count = 1, DescriptorBindingFlags bindingFlags = 0)
    {
        if (_bindings.TryGetValue(binding, out var existing))
        {
            existing.stageFlags |= stages.ToVk();
            _flags[binding] |= bindingFlags.ToVk();
            return this;
        }


        var newBinding = new VkDescriptorSetLayoutBinding
        {
            stageFlags = stages.ToVk(),
            descriptorType = type.ToVk(),
            descriptorCount = count,
            binding = binding
        };

        _bindings.Add(binding, newBinding);
        _flags.Add(binding, bindingFlags.ToVk());
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
        var device = SGraphicsModule.Get().GetDevice();
        var bindings = new List<VkDescriptorSetLayoutBinding>();

        var allFlags = new List<VkDescriptorBindingFlags>();

        VkDescriptorSetLayoutCreateFlags setLayoutCreateFlags = 0;
        foreach (var (binding, info) in _bindings)
        {
            var flags = _flags[binding];
            bindings.Add(info);
            allFlags.Add(flags);
            if ((flags & VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT) != 0)
                setLayoutCreateFlags |= VkDescriptorSetLayoutCreateFlags
                    .VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT;
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

        return SGraphicsModule.Get().GetDescriptorSetLayout(createInfo);
    }
}