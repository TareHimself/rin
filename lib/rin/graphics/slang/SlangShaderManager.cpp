#include "rin/graphics/slang/SlangShaderManager.h"

#include "rin/core/macros.h"
#include "rin/core/utils.h"
#include "rin/graphics/utils.h"
#include "rin/graphics/slang/SlangCompilationContext.h"
#include "rin/graphics/slang/SlangGraphicsShader.h"

namespace rin::graphics
{
    void SlangShaderManager::OnDispose()
    {
    }


    SlangShaderManager::SlangShaderManager()
    {
        slang::createGlobalSession(_globalSession.writeRef());
        slang::TargetDesc targetDesc{};
        targetDesc.format = SLANG_SPIRV;
        targetDesc.profile = _globalSession->findProfile("spirv_1_5");

        std::string searchPath = getBuiltInShadersPath().parent_path().string();

        slang::SessionDesc sessionDesc{};
    
        sessionDesc.targets = &targetDesc;
        sessionDesc.targetCount = 1;
        auto searchPathCStr = searchPath.c_str();
        sessionDesc.searchPaths = &searchPathCStr;
        sessionDesc.searchPathCount = 1;

        _globalSession->createSession(sessionDesc,_defaultSession.writeRef());
    }

    IGraphicsShader* SlangShaderManager::GraphicsFromFile(const std::filesystem::path& path)
    {
        auto absPath = std::filesystem::absolute(path);
        std::string key(absPath.string());
        if(_graphicsShaders.contains(key))
        {
            return _graphicsShaders[key].get();
        }

        return _graphicsShaders.emplace(key,new SlangGraphicsShader(this,absPath)).first->second.get();
    }

    IComputeShader* SlangShaderManager::ComputeFromFile(const std::filesystem::path& path)
    {
        NOT_IMPLEMENTED
    }

    IShaderCompilationContext* SlangShaderManager::CreateCompilationContext(IShader* shader)
    {
        return new SlangCompilationContext(this);
    }

    slang::IGlobalSession* SlangShaderManager::GetGlobalSession()
    {
        return _globalSession;
    }

    slang::ISession* SlangShaderManager::GetDefaultSession()
    {
        return _defaultSession;
    }

    void SlangShaderManager::ReflectVariable(std::unordered_map<std::string, ShaderResource>& resources,
        std::unordered_map<std::string, ShaderPushConstant>& pushConstants, slang::VariableLayoutReflection* variable,vk::ShaderStageFlagBits stage)
    {
        std::string name{variable->getName()};
        if(resources.contains(name))
        {
            resources.at(name).stages |= stage;
        }
        else
        {
            auto categoryCount = variable->getCategoryCount();
            if(categoryCount == 1 && static_cast<SlangParameterCategory>(variable->getCategoryByIndex(0)) == SLANG_PARAMETER_CATEGORY_DESCRIPTOR_TABLE_SLOT)
            {
                ShaderResource resource{};
                resource.name = name;
                resource.stages = stage;

                // Attribute Parsing
                for(auto j = 0; j < variable->getVariable()->getUserAttributeCount(); j++)
                {
                    auto attribute = variable->getVariable()->getUserAttributeByIndex(j);
                    
                    std::string attributeName{attribute->getName()};
                                
                    if(attributeName == "Partial")
                    {
                        resource.bindingFlags |= vk::DescriptorBindingFlagBits::ePartiallyBound;
                    }else if(attributeName == "UpdateAfterBind")
                    {
                        resource.bindingFlags |= vk::DescriptorBindingFlagBits::eUpdateAfterBind;
                    }
                    else if(attributeName == "Variable")
                    {
                        resource.bindingFlags |= vk::DescriptorBindingFlagBits::eVariableDescriptorCount;
                        int count;
                        attribute->getArgumentValueInt(0,&count);
                        resource.count = count;
                    }
                }
                resource.set = variable->getBindingIndex(); 
                resource.binding = variable->getBindingSpace(SLANG_PARAMETER_CATEGORY_DESCRIPTOR_TABLE_SLOT);
                
                resources.emplace(name,resource);
            }
            else if(categoryCount > 1 && (variable->getCategory() & SLANG_PARAMETER_CATEGORY_UNIFORM) == 1)
            {
                ShaderPushConstant push{};
                push.name = name;
                //auto offset = variable->getOffset(SLANG_PARAMETER_CATEGORY_UNIFORM);
                push.size = variable->getTypeLayout()->getSize();
            }
        }
    }

    void SlangShaderManager::Reflect(std::unordered_map<std::string, ShaderResource>& resources,
        std::unordered_map<std::string, ShaderPushConstant>& pushConstants,slang::ProgramLayout* programLayout, slang::EntryPointReflection* entryPoint,
        vk::ShaderStageFlagBits stage)
    {
        for(auto i = 0; i < programLayout->getParameterCount(); i++)
        {
            auto parameter = programLayout->getParameterByIndex(i);
            ReflectVariable(resources,pushConstants,parameter,stage);
        }

        for(auto i = 0; i < entryPoint->getParameterCount(); i++)
        {
            auto parameter = entryPoint->getParameterByIndex(i);
            ReflectVariable(resources,pushConstants,parameter,stage);
        }
    }
}
