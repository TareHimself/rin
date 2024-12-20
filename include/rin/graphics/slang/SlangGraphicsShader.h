#pragma once
#include "rin/graphics/IGraphicsShader.h"
#include "rin/io/Task.h"

namespace rin::graphics
{
    class SlangGraphicsShader : public IGraphicsShader
    {
    protected:
        void OnDispose() override;

    public:
        SlangGraphicsShader(SlangShaderManager * manager,const std::filesystem::path& path);
        std::unordered_map<std::string, ShaderResource>* GetResources() override;
        std::unordered_map<std::string, ShaderPushConstant>* GetPushConstants() override;
        bool Bind(const vk::CommandBuffer& cmd, bool wait) override;
        std::unordered_map<uint32_t, vk::DescriptorSetLayout>* GetSetLayouts() override;
        vk::PipelineLayout GetPipelineLayout() override;

    protected:
        void Compile(IShaderCompilationContext* context) override;

    private:
        io::Task<> _compilationTask{};
        std::unordered_map<std::string,ShaderResource> _resources{};
        std::unordered_map<std::string,ShaderPushConstant> _pushConstants{};
        std::unordered_map<uint32_t, vk::DescriptorSetLayout> _setLayouts{};
        std::vector<vk::ShaderStageFlagBits> _stages{};
        std::vector<vk::ShaderEXT> _shaders{};
        vk::PipelineLayout _pipelineLayout{};
        std::filesystem::path _sourcePath;
    public:
    
    };
}

