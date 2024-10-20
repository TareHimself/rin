#pragma once
#include <filesystem>
#include <rsl/nodes.hpp>
#include <glslang/Include/glslang_c_interface.h>
#include <glslang/Public/ShaderLang.h>

#include "CompiledShader.hpp"
#include "rin/core/BackgroundThread.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/graphics/GraphicsModule.hpp"

class Shader;
class GraphicsShader;
class ShaderManager;

class GlslShaderIncluder : public glslang::TShader::Includer
{
    std::filesystem::path sourceFilePath;
    std::set<IncludeResult*> _results;
    ShaderManager* _manager = nullptr;
    bool _bDebug = false;

public:
    GlslShaderIncluder(ShaderManager* manager, const std::filesystem::path& inPath);


    IncludeResult* includeSystem(const char* filePath, const char* includerName, size_t inclusionDepth) override;
    IncludeResult* includeLocal(const char* filePath, const char* includerName, size_t inclusionDepth) override;

    void releaseInclude(IncludeResult* result) override;

    ~GlslShaderIncluder() override;
};

class ShaderManager : public Disposable
{
    struct CompileTask
    {
        Shared<Shader> shader{};
        CompileTask(const Shared<Shader>& inShader);
    };

    GraphicsModule* _graphicsModule = nullptr;
    std::unordered_map<std::string, std::vector<uint32_t>> _spirv{};
    BackgroundThread<CompiledShader> _compilationThread{};

    glslang_resource_t _resources{};
    bool _init = false;

public:
    DEFINE_DELEGATE_LIST(onShaderCompiled, const std::string&, const vk::ShaderEXT&)
    ShaderManager(GraphicsModule* graphicsModule);

    GraphicsModule* GetGraphicsModule() const;
    std::vector<uint32_t> CompileAstToSpirv(const std::string& id, vk::ShaderStageFlagBits stage,
                                            const std::shared_ptr<rsl::ModuleNode>& node);

    static glslang_stage_t GetLangFromScopeType(rsl::EScopeType scopeType);
    static glslang_stage_t GetLangFromStage(vk::ShaderStageFlagBits stage);

    std::shared_future<CompiledShader> StartShaderCompilation(const Shared<Shader>& shader);
    //std::future<vk::ShaderEXT> CompileShader()

    DEFINE_DELEGATE_LIST(onDispose)
    void Init();
    void OnDispose(bool manual) override;
};
