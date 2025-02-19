using rin.Framework.Core;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics.Descriptors;

public class DescriptorLayoutFactory : Factory<VkDescriptorSetLayout, VkDescriptorSetLayoutCreateInfo, string>
{
    protected override void OnDispose(bool isManual)
    {
        var device = SGraphicsModule.Get().GetDevice();
        unsafe
        {
            foreach (var (_, layout) in GetData()) vkDestroyDescriptorSetLayout(device, layout, null);
        }
    }

    protected override string ToInternalKey(VkDescriptorSetLayoutCreateInfo key)
    {
        var result = ((int)key.flags).ToString();

        unsafe
        {
            for (var i = 0; i < key.bindingCount; i++)
            {
                var binding = key.pBindings[i];
                result +=
                    $"{binding.binding}${binding.descriptorCount}${(int)binding.descriptorType}{(int)binding.stageFlags}";
            }
        }

        return result;
    }

    protected override VkDescriptorSetLayout CreateNew(VkDescriptorSetLayoutCreateInfo key, string internalKey)
    {
        VkDescriptorSetLayout layout;
        unsafe
        {
            var device = SGraphicsModule.Get().GetDevice();
            vkCreateDescriptorSetLayout(device, &key, null, &layout);
        }

        return layout;
    }
}