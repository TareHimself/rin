#pragma once
#include "CompiledShader.hpp"
#include "Shader.hpp"

class GraphicsShader : public Shader
{
    std::string _vertexShaderId{};
    std::string _fragmentShaderId{};
    std::shared_ptr<ashl::ModuleNode> _vertexShader{};
    std::shared_ptr<ashl::ModuleNode> _fragmentShader{};
    std::shared_future<CompiledShader> _compiledShader{};
    static std::unordered_map<std::string,Shared<GraphicsShader>> _shaders;
public:
    GraphicsShader(ShaderManager * manager,const Shared<ashl::ModuleNode>& inVertex,const Shared<ashl::ModuleNode>& inFragment);
        
    bool Bind(const vk::CommandBuffer& cmd, bool wait) override;

        

    std::string GetVertexShaderId() const;
    std::string GetFragmentShaderId() const;
    std::shared_ptr<ashl::ModuleNode> GetVertexShader() const;
    std::shared_ptr<ashl::ModuleNode> GetFragmentShader() const;
    static Shared<GraphicsShader> FromFile(const std::filesystem::path& path);
    void Init();

    CompiledShader Compile(ShaderManager* manager) override;

    std::map<uint32_t, vk::DescriptorSetLayout> GetDescriptorSetLayouts() override;
    vk::PipelineLayout GetPipelineLayout() override;
};
