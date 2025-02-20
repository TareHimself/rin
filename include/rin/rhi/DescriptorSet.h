#pragma once
#include <unordered_map>
#include <vector>
#include <rin/core/Disposable.h>
#include <vulkan/vulkan.hpp>
#include "rin/core/memory.h"

namespace rin::rhi
{
    class DescriptorSet
    {
        struct ResourceArray final : Disposable
        {
        protected:
            void OnDispose() override;
        public:
            std::vector<Shared<Disposable>> resources{};
        };
        vk::DescriptorSet _set{};
        std::unordered_map<uint32_t,Shared<Disposable>> _resources{};
    public:
        explicit DescriptorSet(const vk::DescriptorSet& set);
        vk::DescriptorSet GetDescriptorSet() const;
    };
}
