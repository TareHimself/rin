#include "rin/rhi/slang/SlangShaderManager.h"
#include "rin/rhi/slang/SlangGraphicsShader.h"
#include <iostream>
#include "rin/core/GRuntime.h"
#include "rin/core/utils.h"
#include "rin/rhi/DescriptorLayoutBuilder.h"
#include "rin/rhi/GraphicsModule.h"
#include "rin/rhi/slang/SlangCompilationContext.h"

namespace rin::rhi
{
    void SlangGraphicsShader::OnDispose()
    {
        const auto device = GraphicsModule::Get()->GetDevice();
        device.destroyPipelineLayout(_pipelineLayout);
        for(const auto& shader : _shaders)
        {
            device.destroyShaderEXT(shader,nullptr,GraphicsModule::GetDispatchLoader());
        }
    }

    SlangGraphicsShader::SlangGraphicsShader(SlangShaderManager* manager, const std::filesystem::path& path)
    {
        _sourcePath = path;
        _compilationTask = manager->StartCompilation(this);
    }

    std::unordered_map<std::string,ShaderResource>* SlangGraphicsShader::GetResources()
    {
        return &_resources;
    }

    std::unordered_map<std::string,ShaderPushConstant>* SlangGraphicsShader::GetPushConstants()
    {
        return &_pushConstants;
    }

    bool SlangGraphicsShader::Bind(const vk::CommandBuffer& cmd, bool wait)
    {
        if(!_compilationTask)
        {
            throw std::runtime_error("Shader Is in an invalid state");
        }

        if(wait || _compilationTask->HasException())
        {
            _compilationTask->Wait();
        }

        if(!_compilationTask->HasResult())
        {
            return false;
        }

        cmd.bindShadersEXT(_stages,_shaders,rin::rhi::GraphicsModule::GetDispatchLoader());

        return true;
    }

    std::unordered_map<uint32_t,vk::DescriptorSetLayout>* SlangGraphicsShader::GetSetLayouts()
    {
        return &_descriptorLayouts;
    }

    vk::PipelineLayout SlangGraphicsShader::GetPipelineLayout()
    {
        return _pipelineLayout;
    }

    void SlangGraphicsShader::Compile(IShaderCompilationContext* context)
    {
        //IGraphicsShader::Compile(context);
        if(auto slangCompileContext = dynamic_cast<SlangCompilationContext*>(context))
        {
            auto session = slangCompileContext->manager->GetDefaultSession();

            Slang::ComPtr<slang::IModule> slangModule;
            {
                Slang::ComPtr<slang::IBlob> diagnostics{};
                // const char* moduleName = "shortest";
                // const char* modulePath = "shortest.slang";
                std::string sourceData = readFileAsString(_sourcePath);
                slangModule = session->loadModuleFromSourceString(_sourcePath.string().c_str(),
                                                                  _sourcePath.string().c_str(),sourceData.c_str(),
                                                                  diagnostics.writeRef());

                if(diagnostics)
                {
                    // Do something in the future
                    std::string message{static_cast<const char*>(diagnostics->getBufferPointer()), diagnostics->getBufferSize()};
                    std::cout << "[slang]: " << message << std::endl;
                }

                if(!slangModule)
                {
                    throw std::runtime_error("failed to load module");
                }
            }


            std::vector<Slang::ComPtr<slang::IEntryPoint>> entryPoints{};
            {
                {
                    Slang::ComPtr<slang::IEntryPoint> entryPoint{};

                    slangModule->findEntryPointByName("vertex",entryPoint.writeRef());
                    if(!entryPoint)
                    {
                        throw std::runtime_error("Failed to find vertex entry point");
                    }
                    entryPoints.push_back(entryPoint);
                }

                {
                    Slang::ComPtr<slang::IEntryPoint> entryPoint{};

                    slangModule->findEntryPointByName("fragment",entryPoint.writeRef());
                    if(!entryPoint)
                    {
                        throw std::runtime_error("Failed to find fragment entry point");
                    }
                    entryPoints.push_back(entryPoint);
                }
            }

            std::vector<std::pair<vk::ShaderStageFlagBits,Slang::ComPtr<slang::IBlob>>> spirv{};
            for(auto& entryPoint : entryPoints)
            {
                std::array<slang::IComponentType*,2> componentTypes =
                {
                    slangModule,
                    entryPoint
                };

                Slang::ComPtr<slang::IComponentType> composedProgram;
                {
                    Slang::ComPtr<slang::IBlob> diagnostics;
                    auto operationResult = session->createCompositeComponentType(
                        componentTypes.data(),
                        componentTypes.size(),
                        composedProgram.writeRef(),
                        diagnostics.writeRef());

                    if(diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()), diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl;
                    }

                    if(SLANG_FAILED(operationResult))
                    {
                        throw std::runtime_error("Failed to create composite component");
                    }
                }

                Slang::ComPtr<slang::IComponentType> linkedProgram;
                {
                    Slang::ComPtr<slang::IBlob> diagnostics;
                    auto operationResult = composedProgram->link(
                        linkedProgram.writeRef(),
                        diagnostics.writeRef());

                    if(diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()), diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl;
                    }

                    if(SLANG_FAILED(operationResult))
                    {
                        throw std::runtime_error("Failed to link");
                    }
                }

                Slang::ComPtr<slang::IBlob> generatedCode{};
                {
                    Slang::ComPtr<slang::IBlob> diagnostics;
                    auto operationResult = linkedProgram->getEntryPointCode(
                        0, // entryPointIndex
                        0, // targetIndex
                        generatedCode.writeRef(),
                        diagnostics.writeRef());

                    //linkedProgram->getLayout()->toJson(generatedCode.writeRef());

                    if(diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()), diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl;
                    }

                    if(SLANG_FAILED(operationResult))
                    {
                        throw std::runtime_error("Failed to generate code");
                    }
                }

                
                auto reflectionEntryPoint = linkedProgram->getLayout()->getEntryPointByIndex(0);
                linkedProgram->getLayout()->toJson()
                vk::ShaderStageFlagBits stage = vk::ShaderStageFlagBits::eVertex;
                switch(reflectionEntryPoint->getStage())
                {
                case SLANG_STAGE_VERTEX:
                    stage = vk::ShaderStageFlagBits::eVertex;
                    break;
                case SLANG_STAGE_FRAGMENT:
                    stage = vk::ShaderStageFlagBits::eFragment;
                    break;
                case SLANG_STAGE_COMPUTE:
                    stage = vk::ShaderStageFlagBits::eCompute;
                    break;
                default: ;
                }

                spirv.emplace_back(stage,generatedCode);

                // Slang::ComPtr<slang::IBlob> reflectionJson{};
                // linkedProgram->getLayout()->toJson(reflectionJson.writeRef());
                //
                // if(reflectionJson)
                // {
                //     std::cout << static_cast<const char*>(reflectionJson->getBufferPointer()) << std::endl;
                // }
                SlangShaderManager::Reflect(_resources,_pushConstants,linkedProgram->getLayout(),reflectionEntryPoint,stage);
            }


            DescriptorLayoutBuilder layoutBuilder{};
            std::vector<vk::PushConstantRange> pushConstantRanges{};
            std::map<uint32_t,DescriptorLayoutBuilder> builders{};

            for(auto& [name, set, binding, count, type, stages, bindingFlags, size] : _resources | std::views::values)
            {
                if(!builders.contains(set))
                    builders.emplace(set,
                                     DescriptorLayoutBuilder{});

                builders.at(set).AddBinding(binding,type,stages,count,bindingFlags);
            }

            for(auto& [name, stages, size] : _pushConstants | std::views::values)
            {
                pushConstantRanges.emplace_back(stages,0,static_cast<uint32_t>(size));
            }

            auto max = builders.empty() ? 0 : builders.rbegin()->first;
            std::vector<vk::DescriptorSetLayout> layouts{};
            layouts.reserve(max);
            for(auto i = 0; i < max + 1; i++)
            {
                auto newLayout = builders.contains(i) ? builders[i].Build() : DescriptorLayoutBuilder{}.Build();
                _descriptorLayouts.emplace(i,newLayout);
                layouts.push_back(newLayout);
            }

            auto device = GraphicsModule::Get()->GetDevice();
            {
                vk::PipelineLayoutCreateInfo createInfo{};
                createInfo.setSetLayouts(layouts);
                createInfo.setPushConstantRanges(pushConstantRanges);

                _pipelineLayout = device.createPipelineLayout(createInfo);
            }

            for(auto& [stages,codeBlob] : spirv)
            {
                auto createInfo = vk::ShaderCreateInfoEXT{
                    {},
                    stages,
                    stages == vk::ShaderStageFlagBits::eVertex ? vk::ShaderStageFlagBits::eFragment : static_cast<vk::ShaderStageFlagBits>(0),
                    vk::ShaderCodeTypeEXT::eSpirv,
                    codeBlob->getBufferSize(),
                    codeBlob->getBufferPointer(),
                    "main",
                    static_cast<unsigned int>(layouts.size()),
                    layouts.data(),
                    static_cast<unsigned int>(pushConstantRanges.size()),
                    pushConstantRanges.data()
                };

                auto result = device.createShaderEXT(createInfo,nullptr,GraphicsModule::GetDispatchLoader());
                if(result.result != vk::Result::eSuccess)
                {
                    throw std::runtime_error("Failed to compile shader");
                }

                _shaders.emplace_back(result.value);
                _stages.emplace_back(stages);
            }
        }
    }
}
