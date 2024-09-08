#pragma once
#include "Shader.hpp"

namespace aerox::graphics
{
    class GraphicsShader : public Shader
    {
        std::string _vertexShaderId{};
        std::string _fragmentShaderId{};
        std::shared_ptr<ashl::ModuleNode> _vertexShader{};
        Shared<ashl::ModuleNode> _fragmentShader{};
        std::shared_future<vk::ShaderEXT> _compiledVertexShader{};
        std::shared_future<vk::ShaderEXT> _compiledFragmentShader{};
        static std::unordered_map<std::string,Shared<GraphicsShader>> _shaders;
    public:
        GraphicsShader(ShaderManager * manager,const Shared<ashl::ModuleNode>& inVertex,const Shared<ashl::ModuleNode>& inFragment);
        
        bool Bind(const vk::CommandBuffer& cmd, bool wait) override;

        std::string GetVertexShaderId() const;
        std::string GetFragmentShaderId() const;
        std::shared_ptr<ashl::ModuleNode> GetVertexShader() const;
        std::shared_ptr<ashl::ModuleNode> GetFragmentShader() const;
        static Shared<GraphicsShader> FromFile(const std::filesystem::path& path);
    };
}
