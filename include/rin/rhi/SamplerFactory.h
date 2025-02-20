#pragma once
#include <vulkan/vulkan.hpp>

#include "SamplerInfo.h"
#include "rin/core/Factory.h"

namespace rin::rhi
{
    class SamplerFactory final : public Factory<vk::Sampler, uint64_t, SamplerInfo>
    {
    public:
        vk::Sampler Create(const SamplerInfo& key) override;
        unsigned long long ToInternalKey(const SamplerInfo& key) override;

    };
}
