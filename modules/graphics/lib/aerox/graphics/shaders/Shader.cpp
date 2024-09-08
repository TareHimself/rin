#include "aerox/graphics/shaders/Shader.hpp"
#include "aerox/core/utils.hpp"
#include "ashl/utils.hpp"

namespace aerox::graphics
{
    ShaderManager* Shader::GetManager() const
    {
        return _shaderManager;
    }

    Shader::Texture::Texture(const std::string& inName, const uint32_t& inSet, const uint32_t& inBinding,
                             const uint32_t& inCount)
    {
        name = inName;
        set = inSet;
        binding = inBinding;
        count = inCount;
        
    }

    Shader::Buffer::Buffer(const std::string& inName, const uint32_t& inSet, const uint32_t& inBinding,
        const uint32_t& inSize, const uint32_t& inCount)
    {
        name = inName;
        set = inSet;
        binding = inBinding;
        size = inSize;
        count = inCount;
    }

    Shader::PushConstant::PushConstant(const std::string& inName, const uint64_t& inSize,
                                             const vk::ShaderStageFlags& inStages)
    {
        name = inName;
        size = inSize;
        stages = inStages;
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

    // Shader::Shader(const Shared<ashl::ModuleNode>& inModule, ashl::EScopeType inScopeType) : Shader(inModule,inScopeType,std::to_string(module->ComputeHash()))
    // {
    //     
    // }
    //
    // Shader::Shader(const Shared<ashl::ModuleNode>& inModule, ashl::EScopeType inScopeType,
    //     const std::string& inId)
    // {
    //     module = inModule;
    //     scopeType = inScopeType;
    //     _cachedId = inId;
    //
    //     for (auto &node : module->statements)
    //     {
    //         switch (node->nodeType)
    //         {
    //         case ashl::ENodeType::Layout:
    //             {
    //                 if(auto asLayout = std::dynamic_pointer_cast<ashl::LayoutNode>(node))
    //                 {
    //                     switch (asLayout->declaration->declarationType)
    //                     {
    //                     case ashl::EDeclarationType::Block:
    //                         {
    //                             if(auto asBlock = std::dynamic_pointer_cast<ashl::BlockDeclarationNode>(node))
    //                             {
    //                                 buffers.emplace(asBlock->declarationName,Buffer{
    //                                 asBlock->declarationName,
    //                                 static_cast<unsigned>(ashl::parseInt(asLayout->tags["set"])),
    //                                 static_cast<unsigned>(ashl::parseInt(asLayout->tags["binding"])),
    //                                     static_cast<unsigned>(asBlock->GetSize()),
    //                                 static_cast<unsigned>(asBlock->declarationCount)
    //                             });
    //                             }
    //                         }
    //                         break;
    //                     case ashl::EDeclarationType::Sampler2D:
    //                         {
    //                             textures.emplace(asLayout->declaration->declarationName,Texture{
    //                                 asLayout->declaration->declarationName,
    //                                 static_cast<unsigned>(ashl::parseInt(asLayout->tags["set"])),
    //                                 static_cast<unsigned>(ashl::parseInt(asLayout->tags["binding"])),
    //                                 static_cast<unsigned>(asLayout->declaration->declarationCount)
    //                             });
    //                         }
    //                         break;
    //                     default:
    //                         break;
    //                     }
    //                 }
    //             }
    //             break;
    //         case ashl::ENodeType::PushConstant:
    //             {
    //                 if(auto asPushConstant = std::dynamic_pointer_cast<ashl::PushConstantNode>(node))
    //                 {
    //                     uint64_t pushSize = 0;
    //                     for (auto &declarationNode : asPushConstant->declarations)
    //                     {
    //                         pushSize += declarationNode->GetSize();
    //                     }
    //                     std::string name = "push";
    //                     pushConstants.emplace(name,PushConstant{name,pushSize,ScopeTypeToStageFlags(scopeType)});
    //                 }
    //             }
    //             break;
    //             default:
    //                 break;
    //         }
    //     } 
    // }
}
