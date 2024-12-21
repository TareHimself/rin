#include "rin/graphics/slang/SlangShaderManager.h"

#include "rin/core/macros.h"
#include "rin/graphics/slang/SlangGraphicsShader.h"

#include <iostream>

#include "rin/core/GRuntime.h"
#include "rin/core/utils.h"
#include "rin/graphics/GraphicsModule.h"
#include "rin/graphics/slang/SlangCompilationContext.h"

namespace rin::graphics
{
    void SlangGraphicsShader::OnDispose()
    {
    }

    SlangGraphicsShader::SlangGraphicsShader(SlangShaderManager* manager, const std::filesystem::path& path)
    {
        _sourcePath = path;
        _compilationTask = manager->StartCompilation(this);
    }

    std::unordered_map<std::string, ShaderResource>* SlangGraphicsShader::GetResources()
    {
        return &_resources;
    }

    std::unordered_map<std::string, ShaderPushConstant>* SlangGraphicsShader::GetPushConstants()
    {
        return &_pushConstants;
    }

    bool SlangGraphicsShader::Bind(const vk::CommandBuffer& cmd, bool wait)
    {
        if (!_compilationTask)
        {
            throw std::runtime_error("Shader Is in an invalid state");
        }

        if (wait || _compilationTask->HasException())
        {
            _compilationTask->Wait();
        }

        if (!_compilationTask->HasResult())
        {
            return false;
        }

        cmd.bindShadersEXT(_stages, _shaders, rin::graphics::GraphicsModule::GetDispatchLoader());

        return true;
    }

    std::unordered_map<uint32_t, vk::DescriptorSetLayout>* SlangGraphicsShader::GetSetLayouts()
    {
        return &_setLayouts;
    }

    vk::PipelineLayout SlangGraphicsShader::GetPipelineLayout()
    {
        return _pipelineLayout;
    }

    void SlangGraphicsShader::Compile(IShaderCompilationContext* context)
    {
        //IGraphicsShader::Compile(context);
        if (auto slangCompileContext = dynamic_cast<SlangCompilationContext*>(context))
        {
            auto session = slangCompileContext->manager->GetDefaultSession();

            Slang::ComPtr<slang::IModule> slangModule;
            {
                Slang::ComPtr<slang::IBlob> diagnostics{};
                // const char* moduleName = "shortest";
                // const char* modulePath = "shortest.slang";
                std::string sourceData = readFileAsString(_sourcePath);
                slangModule = session->loadModuleFromSourceString(_sourcePath.string().c_str(),
                                                                  _sourcePath.string().c_str(), sourceData.c_str(),
                                                                  diagnostics.writeRef());

                if (diagnostics)
                {
                    // Do something in the future
                    std::string message{static_cast<const char*>(diagnostics->getBufferPointer()),diagnostics->getBufferSize()};
                    std::cout << "[slang]: " << message << std::endl; 
                }

                if (!slangModule)
                {
                    throw std::runtime_error("failed to load module");
                }
            }

            
            std::vector<Slang::ComPtr<slang::IEntryPoint>> entryPoints{};
            {
                {
                    Slang::ComPtr<slang::IEntryPoint> entryPoint{};

                    slangModule->findEntryPointByName("vertex", entryPoint.writeRef());
                    if (!entryPoint)
                    {
                        throw std::runtime_error("Failed to find vertex entry point");
                    }
                    entryPoints.push_back(entryPoint);
                }

                {
                    Slang::ComPtr<slang::IEntryPoint> entryPoint{};

                    slangModule->findEntryPointByName("fragment", entryPoint.writeRef());
                    if (!entryPoint)
                    {
                        throw std::runtime_error("Failed to find fragment entry point");
                    }
                    entryPoints.push_back(entryPoint);
                }
            }

            std::vector<std::pair<vk::ShaderStageFlagBits,Slang::ComPtr<slang::IBlob>>> spirv{};
            for (auto &entryPoint : entryPoints)
            {
                std::array<slang::IComponentType*, 2> componentTypes =
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

                    if (diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()),diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl; 
                    }

                    if (SLANG_FAILED(operationResult))
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

                    if (diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()),diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl; 
                    }

                    if (SLANG_FAILED(operationResult))
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

                    if (diagnostics)
                    {
                        // Do something in the future
                        std::string message{static_cast<const char*>(diagnostics->getBufferPointer()),diagnostics->getBufferSize()};
                        std::cout << "[slang]: " << message << std::endl; 
                    }

                    if (SLANG_FAILED(operationResult))
                    {
                        throw std::runtime_error("Failed to generate code");
                    }
                }

                auto reflectionEntryPoint = linkedProgram->getLayout()->getEntryPointByIndex(0);
                vk::ShaderStageFlagBits stage = vk::ShaderStageFlagBits::eVertex;
                switch (reflectionEntryPoint->getStage())
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
                
                spirv.emplace_back(std::make_pair(stage,generatedCode));
                
                Slang::ComPtr<slang::IBlob> reflectionJson{};
                linkedProgram->getLayout()->toJson(reflectionJson.writeRef());

                if(reflectionJson)
                {
                    std::cout << static_cast<const char*>(reflectionJson->getBufferPointer()) << std::endl;
                }
                SlangShaderManager::Reflect(_resources,_pushConstants,linkedProgram->getLayout(),reflectionEntryPoint,stage);
            }

            for (auto &value : spirv)
            {
                
            }
        }
    }
}
