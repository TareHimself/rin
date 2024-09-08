#include "aerox/graphics/shaders/GraphicsShader.hpp"

#include <ashl/utils.hpp>

#include "ashl/tokenizer.hpp"
#include "ashl/parser.hpp"
namespace aerox::graphics
{

    std::unordered_map<std::string,Shared<GraphicsShader>> GraphicsShader::_shaders = {};
    
    GraphicsShader::GraphicsShader(ShaderManager* manager, const Shared<ashl::ModuleNode>& inVertex,
        const Shared<ashl::ModuleNode>& inFragment) : Shader(manager)
    {
        _vertexShader = inVertex;
        _fragmentShader = inFragment;
        _vertexShaderId = std::to_string(_vertexShader->ComputeHash());
        _fragmentShaderId = std::to_string(_fragmentShader->ComputeHash());

        _compiledVertexShader = manager->StartShaderCompilation(_vertexShaderId,_vertexShader,ashl::EScopeType::Vertex);
        _compiledFragmentShader = manager->StartShaderCompilation(_fragmentShaderId,_fragmentShader,ashl::EScopeType::Fragment);
    }

    bool GraphicsShader::Bind(const vk::CommandBuffer& cmd, bool wait)
    {
        using namespace std::chrono_literals;
        if(wait)
        {
            _compiledVertexShader.wait();
            _compiledFragmentShader.wait();
        }
        else if(_compiledVertexShader.wait_for(0ms) != std::future_status::ready || _compiledFragmentShader.wait_for(0ms) != std::future_status::ready)
        {
            return false;
        }
        auto vertexShader = _compiledVertexShader.get();
        auto fragmentShader = _compiledFragmentShader.get();
        std::vector<vk::ShaderEXT> shaders = {vertexShader,{},fragmentShader};
        std::vector<vk::ShaderStageFlagBits> stages = {vk::ShaderStageFlagBits::eVertex,vk::ShaderStageFlagBits::eGeometry,vk::ShaderStageFlagBits::eFragment};
        cmd.bindShadersEXT(stages,shaders);
        return true;
    }

    std::string GraphicsShader::GetVertexShaderId() const
    {
        return _vertexShaderId;
    }

    std::string GraphicsShader::GetFragmentShaderId() const
    {
        return _fragmentShaderId;
    }

    std::shared_ptr<ashl::ModuleNode> GraphicsShader::GetVertexShader() const
    {
        return _vertexShader;
    }

    std::shared_ptr<ashl::ModuleNode> GraphicsShader::GetFragmentShader() const
    {
        return _fragmentShader;
    }

    Shared<GraphicsShader> GraphicsShader::FromFile(const std::filesystem::path& path)
    {
        using namespace ashl;
        
        auto fullPath = std::filesystem::absolute(path).string();
        if(_shaders.contains(fullPath)) return _shaders[fullPath];
        auto manager = GraphicsModule::Get()->GetShaderManager();
        auto nodes = tokenize(fullPath);
        auto ast = parse(nodes);
        auto vertex = extractScope(ast,ashl::EScopeType::Vertex);
        auto fragment = extractScope(ast,ashl::EScopeType::Fragment);
        resolveIncludes(vertex);
        resolveIncludes(fragment);
        resolveReferences(vertex);
        resolveReferences(fragment);
        return newShared<GraphicsShader>(manager,vertex,fragment);
    }
}
