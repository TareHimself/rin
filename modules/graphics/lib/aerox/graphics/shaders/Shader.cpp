#include "aerox/graphics/shaders/Shader.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"
#include "ashl/utils.hpp"

namespace aerox::graphics
{
    ShaderManager* Shader::GetManager() const
    {
        return _shaderManager;
    }

    Shader::PushConstant::PushConstant(const std::string& inName, const uint64_t& inSize,
                                       const vk::ShaderStageFlags& inStages)
    {
        name = inName;
        size = inSize;
        stages = inStages;
    }

    void Shader::ComputeResources(
        const std::vector<std::pair<vk::ShaderStageFlags, std::shared_ptr<ashl::ModuleNode>>>& shaders)
    {
        for (auto& [shaderStages,shader] : shaders)
        {
            for (auto& node : shader->statements)
            {
                switch (node->nodeType)
                {
                case ashl::NodeType::Layout:
                    {
                        if (auto asLayout = std::dynamic_pointer_cast<ashl::LayoutNode>(node); asLayout && asLayout->
                            layoutType == ashl::ELayoutType::Uniform)
                        {
                            vk::DescriptorType type{};
                            auto stages = shaderStages;
                            switch (asLayout->declaration->declarationType)
                            {
                            case ashl::EDeclarationType::Block:
                                type = asLayout->tags.contains("$storage")
                                           ? vk::DescriptorType::eStorageBuffer
                                           : vk::DescriptorType::eUniformBuffer;
                                break;
                            case ashl::EDeclarationType::Sampler2D:
                                type = vk::DescriptorType::eCombinedImageSampler;
                                break;
                            case ashl::EDeclarationType::Sampler:
                                type = vk::DescriptorType::eSampler;
                                break;
                            case ashl::EDeclarationType::Texture2D:
                                type = vk::DescriptorType::eSampledImage;
                                break;
                            default:
                                throw std::runtime_error("Unexpected declaration type");
                            }

                            auto set = static_cast<unsigned>(
                                ashl::parseInt(asLayout->tags["set"]));
                            auto binding = static_cast<unsigned>(ashl::parseInt(
                                asLayout->tags["binding"]));

                            vk::DescriptorBindingFlags bindingFlags{};


                            if(asLayout->tags.contains("$update"))
                            {
                                bindingFlags |= vk::DescriptorBindingFlagBits::eUpdateAfterBind;
                            }
                            
                            if (asLayout->tags.contains("$partial"))
                            {
                                bindingFlags |= vk::DescriptorBindingFlagBits::ePartiallyBound;
                            }
                            
                            if (asLayout->tags.contains("$variable"))
                            {
                                bindingFlags |= vk::DescriptorBindingFlagBits::eVariableDescriptorCount;
                            }

                            if (asLayout->tags.contains("$stage") && asLayout->tags["$stage"] == "all")
                            {
                                stages |= vk::ShaderStageFlagBits::eAll;
                            }

                            auto name = asLayout->declaration->declarationName;

                            if (resources.contains(name))
                            {
                                resources.at(name).stages |= stages;
                                resources.at(name).bindingFlags |= bindingFlags;
                            }
                            else
                            {
                                auto count = static_cast<unsigned>(asLayout->declaration->
                                                              declarationCount);
                                if(asLayout->tags.contains("$variable"))
                                {
                                    count = ashl::parseInt(asLayout->tags.at("$variable"));
                                }
                                
                                if (asLayout->declaration->declarationType == ashl::EDeclarationType::Block)
                                {
                                    resources.emplace(name, ShaderResource{
                                                          name,
                                                          set,
                                                          binding,
                                                          count,
                                                          type,
                                                          stages,
                                                          bindingFlags,
                                                          static_cast<uint32_t>(asLayout->declaration->GetSize())
                                                      });
                                }
                                else
                                {
                                    resources.emplace(name, ShaderResource{
                                                          name,
                                                          set,
                                                          binding,
                                                          count,
                                                          type,
                                                          stages,
                                                          bindingFlags
                                                      });
                                }
                            }
                        }
                    }
                    break;
                case ashl::NodeType::PushConstant:
                    {
                        if (auto asPushConstant = std::dynamic_pointer_cast<ashl::PushConstantNode>(node))
                        {
                            std::string name = "push";
                            if (pushConstants.contains(name))
                            {
                                pushConstants.at(name).stages |= shaderStages;
                            }
                            else
                            {
                                uint64_t pushSize = 0;
                                for (auto& declarationNode : asPushConstant->declarations)
                                {
                                    pushSize += declarationNode->GetSize();
                                }

                                pushConstants.emplace(name, PushConstant{name, pushSize, shaderStages});
                            }
                        }
                    }
                    break;
                default:
                    break;
                }
            }
        }
    }

    vk::ShaderStageFlags Shader::ScopeTypeToStageFlags(const ashl::EScopeType& scopeType)
    {
        switch (scopeType)
        {
        case ashl::EScopeType::Fragment:
            return vk::ShaderStageFlagBits::eFragment;
        case ashl::EScopeType::Vertex:
            return vk::ShaderStageFlagBits::eVertex;
        }

        return vk::ShaderStageFlagBits::eVertex;
    }

    Shader::Shader(ShaderManager* manager)
    {
        _shaderManager = manager;
    }

    std::map<uint32_t, vk::DescriptorSetLayout> Shader::ComputeDescriptorSetLayouts() const
    {
        
        std::map<uint32_t, DescriptorLayoutBuilder> builders{};

        for (auto& item : resources | std::views::values)
        {
            if (!builders.contains(item.set))
                builders.emplace(item.set,
                                 DescriptorLayoutBuilder{});

            builders.at(item.set).AddBinding(item.binding,item.type,item.stages,item.count,item.bindingFlags);
        }
        std::map<uint32_t,vk::DescriptorSetLayout> layouts{};
        for (auto &[set,builder] : builders)
        {
            layouts.emplace(set,builder.Build());
        }
        return layouts;
    }

    std::vector<vk::PushConstantRange> Shader::ComputePushConstantRanges() const
    {
        std::vector<vk::PushConstantRange> pushConstantRanges{};
        for (auto& item : pushConstants | std::views::values)
        {
            pushConstantRanges.emplace_back(item.stages, 0, static_cast<uint32_t>(item.size));
        }
        return pushConstantRanges;
    }
}
