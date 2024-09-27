#include "rin/graphics/descriptors/DescriptorLayoutBuilder.hpp"

#include <ranges>
#include <rsl/utils.hpp>

#include "rin/core/GRuntime.hpp"
#include "rin/graphics/GraphicsModule.hpp"

DescriptorLayoutBuilder& DescriptorLayoutBuilder::AddBinding(uint32_t binding, vk::DescriptorType type,
        const vk::ShaderStageFlags& stageFlags, uint32_t count, const vk::DescriptorBindingFlags& flags)
    {
        if(_bindings.contains(binding))
        {
            auto existing = _bindings[binding];
            existing.setStageFlags(existing.stageFlags | stageFlags);
            _bindings.insert_or_assign(binding,existing);
            _flags.insert_or_assign(binding,_flags[binding] | flags);
        }
        else
        {
            _bindings.emplace(binding,vk::DescriptorSetLayoutBinding{binding,type,count,stageFlags});
            _flags.emplace(binding,flags);
        }
        
        return *this;
    }

    DescriptorLayoutBuilder& DescriptorLayoutBuilder::Clear()
    {
        _bindings.clear();
        return *this;
    }

    vk::DescriptorSetLayout DescriptorLayoutBuilder::Build()
    {
        auto store = GRuntime::Get()->GetModule<GraphicsModule>()->GetDescriptorLayoutStore();
        std::vector<vk::DescriptorSetLayoutBinding> bindings{};
        std::vector<vk::DescriptorBindingFlags> allBindingFlags{};
        bindings.reserve(_bindings.size());
        allBindingFlags.reserve(_bindings.size());
        vk::DescriptorSetLayoutCreateFlags createFlags{};
        
        for (auto& binding : _bindings | std::views::values)
        {
            bindings.push_back(binding);
            auto flags = _flags[binding.binding];
            allBindingFlags.push_back(flags);
            if(flags & vk::DescriptorBindingFlagBits::eUpdateAfterBind)
            {
                createFlags |= vk::DescriptorSetLayoutCreateFlagBits::eUpdateAfterBindPool;
            }
        }

        auto pNext = vk::DescriptorSetLayoutBindingFlagsCreateInfo{allBindingFlags};

        auto createInfo = vk::DescriptorSetLayoutCreateInfo{createFlags,bindings,&pNext};

        
        if(const auto result = store->Create(createInfo))
        {
            return result;
        }

        throw std::runtime_error("Failed to create descriptor set");
    }
