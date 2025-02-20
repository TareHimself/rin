#include "rin/rhi/SamplerFactory.h"

#include "rin/core/utils.h"
#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{

    vk::Sampler SamplerFactory::Create(const SamplerInfo& key)
    {
        const auto vkFilter = imageFilterToVulkanFilter(key.filter);
        const auto vkTiling = imageTilingToVulkanAddressMode(key.tiling);
        const vk::SamplerCreateInfo createInfo{
            {},
            vkFilter,
            vkFilter,
            vkFilter == vk::Filter::eLinear ? vk::SamplerMipmapMode::eLinear : vk::SamplerMipmapMode::eNearest,
            vkTiling,
            vkTiling,
            vkTiling
        };

        return GraphicsModule::Get()->GetDevice().createSampler(createInfo);
    }
    unsigned long long SamplerFactory::ToInternalKey(const SamplerInfo& key)
    {
        return hashCombine(static_cast<int>(key.filter),static_cast<int>(key.tiling));
    }
}
