#pragma once
#include "CompiledShader.h"
#include "IShaderCompilationContext.h"
#include "rin/core/Disposable.h"
#include <unordered_map>
#include "ShaderPushConstant.h"
#include "ShaderResource.h"

namespace rin::rhi
{
    class IShader : public Disposable
    {
        friend IShaderManager;
    protected:
        virtual void Compile(IShaderCompilationContext * context) = 0;
    public:

        virtual std::unordered_map<std::string,ShaderResource> * GetResources() = 0;

        virtual std::unordered_map<std::string,ShaderPushConstant> * GetPushConstants() = 0;

        virtual bool Bind(const vk::CommandBuffer& cmd,bool wait = false) = 0;

        virtual std::unordered_map<uint32_t,vk::DescriptorSetLayout> * GetSetLayouts() = 0;

        virtual vk::PipelineLayout GetPipelineLayout() = 0;
    };
}

