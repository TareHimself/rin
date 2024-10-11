#include "rin/graphics/shaders/ShaderManager.hpp"
#include <fstream>
#include <iostream>
#include <VkBootstrap.h>
#include <rsl/glsl.hpp>
#include <rsl/utils.hpp>
#include <glslang/Public/ResourceLimits.h>
#include <glslang/SPIRV/GlslangToSpv.h>
#include "rin/core/utils.hpp"
#include "rin/graphics/descriptors/DescriptorLayoutBuilder.hpp"
#include <glslang/Include/glslang_c_interface.h>
#include <glslang/Public/resource_limits_c.h>

#include "rin/graphics/shaders/Shader.hpp"
#include "rin/graphics/shaders/ShaderCompileError.hpp"

GlslShaderIncluder::GlslShaderIncluder(ShaderManager* manager, const std::filesystem::path& inPath)
{
    _manager = manager;
    sourceFilePath = inPath;
}

glslang::TShader::Includer::IncludeResult* GlslShaderIncluder::includeSystem(const char* filePath,
                                                                             const char* includerName,
                                                                             size_t inclusionDepth)
{
    const std::filesystem::path actualPath(filePath);
    const auto fileContent = readFileAsString(actualPath);
    auto result = new IncludeResult(actualPath.string(), fileContent.c_str(), fileContent.size(), nullptr);
    _results.emplace(result);
    return result;
}

glslang::TShader::Includer::IncludeResult* GlslShaderIncluder::includeLocal(const char* filePath,
                                                                            const char* includerName,
                                                                            size_t inclusionDepth)
{
    const auto actualPath = sourceFilePath.parent_path() / std::filesystem::path(filePath);
    const auto fileContent = readFileAsString(actualPath);
    auto result = new IncludeResult(actualPath.string(), fileContent.c_str(), fileContent.size(), nullptr);
    _results.emplace(result);
    return result;
}

void GlslShaderIncluder::releaseInclude(IncludeResult* result)
{
    if (result != nullptr)
    {
        _results.erase(result);
        delete result;
    }
}

GlslShaderIncluder::~GlslShaderIncluder()
{
    for (const auto result : _results)
    {
        delete result;
    }

    _results.clear();
}

ShaderManager::CompileTask::CompileTask(const Shared<Shader>& inShader)
{
    shader = inShader;
}

ShaderManager::ShaderManager(GraphicsModule* graphicsModule)
{
    _graphicsModule = graphicsModule;
    _resources = *glslang_default_resource();
}

GraphicsModule* ShaderManager::GetGraphicsModule() const
{
    return _graphicsModule;
}

std::vector<uint32_t> ShaderManager::CompileAstToSpirv(const std::string& id, vk::ShaderStageFlagBits stage,
                                                       const std::shared_ptr<rsl::ModuleNode>& node)
{
    if (_spirv.contains(id))
    {
        return _spirv[id];
    }
    auto glslShader =
        "#version 450\n#extension GL_EXT_buffer_reference : require\n#extension GL_EXT_nonuniform_qualifier : require\n#extension GL_EXT_scalar_block_layout : require\n"
        + rsl::glsl::generate(node);
    auto glslStage = GetLangFromStage(stage);
    const glslang_input_t input = {
        .language = GLSLANG_SOURCE_GLSL,
        .stage = glslStage,
        .client = GLSLANG_CLIENT_VULKAN,
        .client_version = GLSLANG_TARGET_VULKAN_1_3,
        .target_language = GLSLANG_TARGET_SPV,
        .target_language_version = GLSLANG_TARGET_SPV_1_5,
        .code = glslShader.c_str(),
        .default_version = 450,
        .default_profile = GLSLANG_NO_PROFILE,
        .force_default_version_and_profile = false,
        .forward_compatible = false,
        .messages = GLSLANG_MSG_DEFAULT_BIT,
        .resource = &_resources,
    };

    glslang_shader_t* shader = glslang_shader_create(&input);

    std::string fileName = "<shader>";
    if (!glslang_shader_preprocess(shader, &input))
    {
        std::string message{glslang_shader_get_info_log(shader)};
        glslang_shader_delete(shader);
        throw ShaderCompileError("Preprocessing Failed: " + message, glslShader);
    }

    if (!glslang_shader_parse(shader, &input))
    {
        std::string message{glslang_shader_get_info_log(shader)};
        glslang_shader_delete(shader);
        throw ShaderCompileError("Parsing Failed: " + message, glslShader);
    }

    glslang_program_t* program = glslang_program_create();
    glslang_program_add_shader(program, shader);

    if (!glslang_program_link(program, GLSLANG_MSG_SPV_RULES_BIT | GLSLANG_MSG_VULKAN_RULES_BIT))
    {
        std::string message{glslang_shader_get_info_log(shader)};
        glslang_program_delete(program);
        glslang_shader_delete(shader);
        throw ShaderCompileError("Linking Failed: " + message, glslShader);
    }

    glslang_program_SPIRV_generate(program, glslStage);

    std::vector<uint32_t> spvResult{};
    spvResult.resize(glslang_program_SPIRV_get_size(program));

    glslang_program_SPIRV_get(program, spvResult.data());
    glslang_program_delete(program);
    glslang_shader_delete(shader);

    _spirv.emplace(id, spvResult);

    return spvResult;
}

glslang_stage_t ShaderManager::GetLangFromScopeType(rsl::EScopeType scopeType)
{
    switch (scopeType)
    {
    case rsl::EScopeType::Vertex:
        return GLSLANG_STAGE_VERTEX;
    case rsl::EScopeType::Fragment:
        return GLSLANG_STAGE_FRAGMENT;
    }

    return GLSLANG_STAGE_VERTEX;
}

glslang_stage_t ShaderManager::GetLangFromStage(vk::ShaderStageFlagBits stage)
{
    if (stage & vk::ShaderStageFlagBits::eVertex) return GLSLANG_STAGE_VERTEX;

    if (stage & vk::ShaderStageFlagBits::eFragment) return GLSLANG_STAGE_FRAGMENT;

    return GLSLANG_STAGE_VERTEX;
}

std::shared_future<CompiledShader> ShaderManager::StartShaderCompilation(const Shared<Shader>& shader)
{
    CompileTask task{shader};
    return _compilationThread.Put([this,task]
    {
        return task.shader->Compile(this);
    });
}

void ShaderManager::Init()
{
    auto systemInfo = vkb::SystemInfo::get_system_info().value();
    auto device = _graphicsModule->GetPhysicalDevice();
    _resources.max_draw_buffers = true;
    _resources.max_combined_image_units_and_fragment_outputs = 128;
    _resources.max_compute_work_group_count_x = 128;
    _resources.max_compute_work_group_count_y = 128;
    _resources.max_compute_work_group_count_z = 128;
    _resources.max_draw_buffers = 10;
    _resources.limits.non_inductive_for_loops = true;
    // _resources.limits.while_loops = true;
    // _resources.limits.do_while_loops = true;
    _resources.limits.general_uniform_indexing = true;
    _resources.limits.general_varying_indexing = true;
    _resources.limits.general_sampler_indexing = true;
    _resources.limits.general_variable_indexing = true;
    _resources.limits.general_constant_matrix_vector_indexing = true;
    _resources.limits.general_attribute_matrix_vector_indexing = true;
    glslang_initialize_process();
}

void ShaderManager::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);
    onDispose->Invoke();
    _compilationThread.WaitForAll();
    _compilationThread.Stop();
    glslang_finalize_process();
}
