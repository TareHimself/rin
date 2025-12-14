using Rin.Framework.Graphics.Vulkan;
using Rin.Framework.Shared;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics;

public class SamplerFactory : Factory<VkSampler, SamplerSpec, SamplerSpec>
{
    private readonly VkDevice _device;

    public SamplerFactory(in VkDevice device)
    {
        _device = device;
    }

    protected override SamplerSpec ToInternalKey(SamplerSpec key)
    {
        return key;
    }

    protected override VkSampler CreateNew(SamplerSpec key, SamplerSpec internalKey)
    {
        var sampler = new VkSampler();
        var vkFilter = key.Filter.ToVk();
        var vkAddressMode = key.Tiling.ToVk();

        var samplerCreateInfo = new VkSamplerCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO,
            magFilter = vkFilter,
            minFilter = vkFilter,
            addressModeU = vkAddressMode,
            addressModeV = vkAddressMode,
            addressModeW = vkAddressMode,
            mipmapMode = key.Filter switch
            {
                ImageFilter.Linear => VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_LINEAR,
                ImageFilter.Nearest => VkSamplerMipmapMode.VK_SAMPLER_MIPMAP_MODE_NEAREST,
                _ => throw new ArgumentOutOfRangeException()
            },
            anisotropyEnable = 0,
            borderColor = VkBorderColor.VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK,
            maxLod = 1000f
        };

        unsafe
        {
            vkCreateSampler(_device, &samplerCreateInfo, null, &sampler);
        }

        return sampler;
    }

    public override void Dispose()
    {
        unsafe
        {
            foreach (var (_, sampler) in GetData()) vkDestroySampler(_device, sampler, null);
        }
    }
}