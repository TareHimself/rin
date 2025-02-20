#pragma once
#include <vulkan/vulkan.hpp>

#include "rin/core/Factory.h"

namespace rin::rhi
{
    class DescriptorLayoutFactory final : public Factory<vk::DescriptorSetLayout, uint64_t, vk::DescriptorSetLayoutCreateInfo>
    {
    protected:
        vk::DescriptorSetLayout Create(const vk::DescriptorSetLayoutCreateInfo& key) override;
        uint64_t ToInternalKey(const vk::DescriptorSetLayoutCreateInfo& key) override;
    };
}
