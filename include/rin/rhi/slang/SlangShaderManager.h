#pragma once
#include "rin/rhi/IShaderManager.h"
#include <slang.h>
#include <slang-com-ptr.h>
#include <slang-com-helper.h>
#include <slang-com-helper.h>
namespace rin::rhi
{
    class SlangShaderManager : public IShaderManager
    {
        Slang::ComPtr<slang::IGlobalSession> _globalSession{};
        Slang::ComPtr<slang::ISession> _defaultSession{};
        std::unordered_map<std::string,Shared<IGraphicsShader>> _graphicsShaders{};
        
    protected:
        void OnDispose() override;

    public:
        SlangShaderManager();
        IGraphicsShader* GraphicsFromFile(const std::filesystem::path& path) override;
        IComputeShader* ComputeFromFile(const std::filesystem::path& path) override;
        IShaderCompilationContext* CreateCompilationContext(IShader* shader) override;
        slang::IGlobalSession * GetGlobalSession();
        slang::ISession * GetDefaultSession();

        static bool VariableIsPushConstant(slang::VariableLayoutReflection * variable);
        static void ReflectVariable(std::unordered_map<std::string, ShaderResource>& resources,std::unordered_map<std::string, ShaderPushConstant>& pushConstants,slang::VariableLayoutReflection * variable,vk::ShaderStageFlagBits stage);
        static void Reflect(std::unordered_map<std::string, ShaderResource>& resources,std::unordered_map<std::string, ShaderPushConstant>& pushConstants,slang::ProgramLayout* programLayout,slang::EntryPointReflection* entryPoint,vk::ShaderStageFlagBits stage);
    };

}
