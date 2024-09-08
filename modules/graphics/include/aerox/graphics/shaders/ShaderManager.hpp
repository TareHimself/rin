#pragma once
#include <filesystem>
#include <ashl/nodes.hpp>
#include <glslang/Include/glslang_c_interface.h>
#include <glslang/Public/ShaderLang.h>
#include "aerox/core/BackgroundThread.hpp"
#include "aerox/core/Disposable.hpp"
#include "aerox/graphics/GraphicsModule.hpp"

namespace aerox::graphics
{
    class GraphicsShader;
    class ShaderManager;
    
    class GlslShaderIncluder : public glslang::TShader::Includer {
        std::filesystem::path sourceFilePath;
        std::set<IncludeResult *> _results;
        ShaderManager * _manager = nullptr;
        bool _bDebug = false;
    public:
        GlslShaderIncluder(ShaderManager * manager,const std::filesystem::path &inPath);


        IncludeResult *includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) override;
        IncludeResult *includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) override;

        void releaseInclude(IncludeResult *result) override;

        ~GlslShaderIncluder() override;
  
    };
    
    class ShaderManager : public Disposable
    {
        struct CompileTask
        {
            std::string id;
            std::shared_ptr<ashl::ModuleNode> shader{};
            ashl::EScopeType scopeType;
            CompileTask(const std::string& inId,const std::shared_ptr<ashl::ModuleNode>& inShader,ashl::EScopeType inScopeType);
        };
        GraphicsModule * _graphicsModule = nullptr;
        std::unordered_map<std::string,vk::ShaderEXT> _vkShaders{};
        std::unordered_map<std::string,std::shared_future<vk::ShaderEXT>> _pendingShaders{};
        std::unordered_map<std::string,Shared<GraphicsShader>> _graphicsShaders{};
        BackgroundThread<vk::ShaderEXT> _compilationThread{};

        glslang_resource_t _resources{};
    public:
        DEFINE_DELEGATE_LIST(onShaderCompiled,const std::string&,const vk::ShaderEXT&)
        ShaderManager(GraphicsModule * graphicsModule);

        vk::ShaderEXT CompileShader(const CompileTask& task);

        static glslang_stage_t GetLangFromScopeType(ashl::EScopeType scopeType);

        std::shared_future<vk::ShaderEXT> StartShaderCompilation(const std::string& id,const std::shared_ptr<ashl::ModuleNode>& shader,ashl::EScopeType scopeType);
        //std::future<vk::ShaderEXT> CompileShader()

        void Init();
        void OnDispose(bool manual) override;
    };
}
