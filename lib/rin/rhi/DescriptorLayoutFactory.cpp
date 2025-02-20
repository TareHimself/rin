#include "rin/rhi/DescriptorLayoutFactory.h"

#include "rin/core/utils.h"
#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{
    vk::DescriptorSetLayout DescriptorLayoutFactory::Create(const vk::DescriptorSetLayoutCreateInfo& key)
    {
        return GraphicsModule::Get()->GetDevice().createDescriptorSetLayout(key);
    }
    uint64_t DescriptorLayoutFactory::ToInternalKey(const vk::DescriptorSetLayoutCreateInfo& key)
    {
        uint64_t id{0};
        auto next = static_cast<const vk::DescriptorSetLayoutBindingFlagsCreateInfo*>(key.pNext);
        for (auto i = 0; i < key.bindingCount; i++)
        {
            auto binding = *(key.pBindings + i);

            id = hashCombine(
                id,
                binding.binding,
                binding.descriptorCount,
                static_cast<int>(binding.descriptorType),
                static_cast<uint32_t>(binding.stageFlags),
                static_cast<uint32_t>(*(next->pBindingFlags + i))
            );
        }

        return id;
    }
}
