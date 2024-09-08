#include "aerox/graphics/shaders/ShaderManager.hpp"
#include <fstream>
#include <iostream>
#include <VkBootstrap.h>
#include <ashl/glsl.hpp>
#include <ashl/utils.hpp>
#include <glslang/Public/ResourceLimits.h>
#include <glslang/SPIRV/GlslangToSpv.h>
#include "aerox/core/utils.hpp"
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"
#include <glslang/Include/glslang_c_interface.h>
#include <glslang/Public/resource_limits_c.h>

namespace aerox::graphics
{
    GlslShaderIncluder::GlslShaderIncluder(ShaderManager* manager, const std::filesystem::path& inPath)
    {
        _manager = manager;
        sourceFilePath = inPath;
    }

    glslang::TShader::Includer::IncludeResult* GlslShaderIncluder::includeSystem(const char* filePath,
        const char* includerName, size_t inclusionDepth)
    {
        const std::filesystem::path actualPath(filePath);
        const auto fileContent = readFileAsString(actualPath);
        auto result = new IncludeResult(actualPath.string(), fileContent.c_str(), fileContent.size(), nullptr);
        _results.emplace(result);
        return result;
    }

    glslang::TShader::Includer::IncludeResult* GlslShaderIncluder::includeLocal(const char* filePath,
        const char* includerName, size_t inclusionDepth)
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

    ShaderManager::CompileTask::CompileTask(const std::string& inId, const std::shared_ptr<ashl::ModuleNode>& inShader,
                                            ashl::EScopeType inScopeType)
    {
        id = inId;
        shader = inShader;
        scopeType = inScopeType;
    }

    ShaderManager::ShaderManager(GraphicsModule* graphicsModule)
    {
        _graphicsModule = graphicsModule;
        _resources = *glslang_default_resource();
    }

    vk::ShaderEXT ShaderManager::CompileShader(const CompileTask& task)
    {
        if(_vkShaders.contains(task.id)) return _vkShaders[task.id];
        
        std::vector<unsigned int> spvResult{};
        
        {

            auto glslShader = "#version 450\n#extension GL_EXT_buffer_reference : require\n#extension GL_EXT_scalar_block_layout : require\n" + ashl::glsl::generate(task.shader);
            auto stage = GetLangFromScopeType(task.scopeType);
            const glslang_input_t input = {
                .language = GLSLANG_SOURCE_GLSL,
                .stage = stage,
                .client = GLSLANG_CLIENT_VULKAN,
                .client_version = GLSLANG_TARGET_VULKAN_1_2,
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
            if (!glslang_shader_preprocess(shader, &input))	{
                // printf("GLSL preprocessing failed %s\n", fileName);
                // printf("%s\n", glslang_shader_get_info_log(shader));
                // printf("%s\n", glslang_shader_get_info_debug_log(shader));
                // printf("%s\n", input.code);
                glslang_shader_delete(shader);
                throw std::runtime_error("Failed to compile shader");
            }

            if (!glslang_shader_parse(shader, &input)) {
                // printf("GLSL parsing failed %s\n", fileName);
                // printf("%s\n", glslang_shader_get_info_log(shader));
                // printf("%s\n", glslang_shader_get_info_debug_log(shader));
                // printf("%s\n", glslang_shader_get_preprocessed_code(shader));
                auto log = glslang_shader_get_info_log(shader);
                auto debugLog = glslang_shader_get_info_debug_log(shader);
                auto shaderCode = glslang_shader_get_preprocessed_code(shader);
                std::cout << log << "\n" << shaderCode << std::endl;
                glslang_shader_delete(shader);
                throw std::runtime_error("Failed to compile shader");
            }

            glslang_program_t* program = glslang_program_create();
            glslang_program_add_shader(program, shader);

            if (!glslang_program_link(program, GLSLANG_MSG_SPV_RULES_BIT | GLSLANG_MSG_VULKAN_RULES_BIT)) {
                // printf("GLSL linking failed %s\n", fileName);
                // printf("%s\n", glslang_program_get_info_log(program));
                // printf("%s\n", glslang_program_get_info_debug_log(program));
                glslang_program_delete(program);
                glslang_shader_delete(shader);
                throw std::runtime_error("Failed to compile shader");
            }

            glslang_program_SPIRV_generate(program, stage);
            
            spvResult.resize(glslang_program_SPIRV_get_size(program));
            glslang_program_SPIRV_get(program, spvResult.data());

            // const char* spirv_messages = glslang_program_SPIRV_get_messages(program);
            // if (spirv_messages)
            //     printf("(%s) %s\b", fileName, spirv_messages);

            glslang_program_delete(program);
            glslang_shader_delete(shader);
        }

        {
            auto stage = task.scopeType == ashl::EScopeType::Vertex
                             ? vk::ShaderStageFlagBits::eVertex
                             : vk::ShaderStageFlagBits::eFragment;
            DescriptorLayoutBuilder layoutBuilder{};
            std::vector<vk::PushConstantRange> pushConstantRanges{};
            std::map<uint32_t, DescriptorLayoutBuilder> builders{};

            for (auto& statement : task.shader->statements)
            {
                if (statement->nodeType == ashl::NodeType::Layout)
                {
                    if (auto layout = std::dynamic_pointer_cast<ashl::LayoutNode>(statement); layout && layout->tags.
                        contains("set") && layout->tags.contains("binding"))
                    {
                        auto set = ashl::parseInt(layout->tags["set"]);
                        auto binding = ashl::parseInt(layout->tags["binding"]);

                        if (!builders.contains(set))
                            builders.emplace(static_cast<uint32_t>(set),
                                             DescriptorLayoutBuilder{});

                        vk::DescriptorBindingFlags flags = vk::DescriptorBindingFlagBits::eUpdateAfterBind;
                        if (layout->tags.contains("$partial"))
                        {
                            flags |= vk::DescriptorBindingFlagBits::ePartiallyBound;
                        }
                        if (layout->tags.contains("$variable"))
                        {
                            flags |= vk::DescriptorBindingFlagBits::eVariableDescriptorCount;
                        }

                        if (layout->declaration->declarationType == ashl::EDeclarationType::Sampler2D)
                        {
                            builders[set].AddBinding(binding, vk::DescriptorType::eSampler, stage, 1, flags);
                        }
                        else if (layout->declaration->declarationType == ashl::EDeclarationType::Block)
                        {
                            builders[set].AddBinding(binding, vk::DescriptorType::eUniformBuffer, stage);
                        }
                    }
                }

                if (statement->nodeType == ashl::NodeType::PushConstant)
                {
                    if (auto pushConstant = std::dynamic_pointer_cast<ashl::PushConstantNode>(statement))
                    {
                        pushConstantRanges.emplace_back(stage, 0, static_cast<uint32_t>(pushConstant->GetSize()));
                    }
                }
            }

            auto max = builders.rbegin()->first;
            std::vector<vk::DescriptorSetLayout> layouts{};
            layouts.reserve(max);
            for (auto i = 0; i < max + 1; i++)
            {
                if (builders.contains(i))
                {
                    layouts.push_back(builders[i].Build());
                }
                else
                {
                    layouts.push_back(DescriptorLayoutBuilder{}.Build());
                }
            }

            auto nextStage = task.scopeType == ashl::EScopeType::Vertex
                                 ? static_cast<int>(vk::ShaderStageFlagBits::eFragment)
                                 : 0;
            auto createInfo = vk::ShaderCreateInfoEXT{
                {},
                stage,
                static_cast<vk::ShaderStageFlagBits>(nextStage),
                vk::ShaderCodeTypeEXT::eBinary,
                spvResult.size(),
                spvResult.data(),
                "main",
                static_cast<unsigned int>(layouts.size()),
                layouts.data(),
                static_cast<unsigned int>(pushConstantRanges.size()),
                pushConstantRanges.data()
            };
            auto device = _graphicsModule->GetDevice();
            auto shader = _graphicsModule->GetDevice().createShaderEXT(createInfo).value;
            for (auto &layout : layouts)
            {
                device.destroyDescriptorSetLayout(layout);
            }
            _vkShaders.emplace(task.id,shader);
            _pendingShaders.erase(task.id);
            return shader;
        }
    }
    
    // vk::ShaderEXT ShaderManager::CompileShader(const CompileTask& task)
    // {
    //     const auto lang = GetLangFromScopeType(task.scopeType);
    //
    //     const auto shader = new glslang::TShader(lang);
    //
    //     auto glslShader = "#version 450\n#extension GL_EXT_buffer_reference : require\n#extension GL_EXT_scalar_block_layout : require\n" + ashl::glsl::generate(task.shader);
    //
    //
    //     shader->setEnvClient(glslang::EShClient::EShClientVulkan, glslang::EShTargetVulkan_1_3);
    //     shader->setEnvTarget(glslang::EshTargetSpv, glslang::EShTargetSpv_1_3);
    //     const char* sourcePtr = glslShader.c_str();
    //     const char* const * sourcePtrArr = &(sourcePtr);
    //     const int sourcePtrSize = static_cast<int>(glslShader.size());
    //
    //     shader->setStringsWithLengths(sourcePtrArr, &sourcePtrSize, 1);
    //     shader->setSourceEntryPoint("main");
    //     shader->setEntryPoint("main");
    //
    //
    //     //shader->getIntermediate()->setSource(glslang::EShSourceGlsl);
    //
    //     constexpr auto message = static_cast<EShMessages>(EShMessages::EShMsgVulkanRules | EShMessages::EShMsgSpvRules |
    //         EShMsgDebugInfo);
    //
    //     GlslShaderIncluder includer(this, "");
    //     
    //     const auto result = shader->parse(&_resources, 450, ENoProfile, false, false, message, includer);
    //     
    //     auto err = std::string(shader->getInfoLog());
    //
    //     if (result)
    //     {
    //         throw std::runtime_error("Failed to parse shader");
    //     }
    //
    //     std::vector<unsigned int> spvResult{};
    //
    //     {
    //         glslang::TProgram program;
    //         program.addShader(shader);
    //
    //         if (!program.link(message))
    //         {
    //             throw std::runtime_error(
    //                 std::string("Failed to parse shader at path: ") + "\n" + program.
    //                 getInfoLog());
    //         }
    //
    //         glslang::SpvOptions options{};
    //
    //         // Needed for reflection
    //         options.generateDebugInfo = true;
    //         options.stripDebugInfo = false;
    //         options.disableOptimizer = false;
    //         options.emitNonSemanticShaderDebugInfo = true;
    //
    //
    //         glslang::GlslangToSpv(*program.getIntermediate(lang), spvResult, &options);
    //         //program.buildReflection(EShReflectionOptions::EShReflectionAllIOVariables);
    //     }
    //
    //     delete shader;
    //
    //     {
    //         auto stage = task.scopeType == ashl::EScopeType::Vertex
    //                          ? vk::ShaderStageFlagBits::eVertex
    //                          : vk::ShaderStageFlagBits::eFragment;
    //         DescriptorLayoutBuilder layoutBuilder{};
    //         std::vector<vk::PushConstantRange> pushConstantRanges{};
    //         std::map<uint32_t, DescriptorLayoutBuilder> builders{};
    //
    //         for (auto& statement : task.shader->statements)
    //         {
    //             if (statement->nodeType == ashl::ENodeType::Layout)
    //             {
    //                 if (auto layout = std::dynamic_pointer_cast<ashl::LayoutNode>(statement); layout && layout->tags.
    //                     contains("set") && layout->tags.contains("binding"))
    //                 {
    //                     auto set = ashl::parseInt(layout->tags["set"]);
    //                     auto binding = ashl::parseInt(layout->tags["binding"]);
    //
    //                     if (!builders.contains(set))
    //                         builders.emplace(static_cast<uint32_t>(set),
    //                                          DescriptorLayoutBuilder{});
    //
    //                     vk::DescriptorBindingFlags flags = vk::DescriptorBindingFlagBits::eUpdateAfterBind;
    //                     if (layout->tags.contains("$partial"))
    //                     {
    //                         flags |= vk::DescriptorBindingFlagBits::ePartiallyBound;
    //                     }
    //                     if (layout->tags.contains("$variable"))
    //                     {
    //                         flags |= vk::DescriptorBindingFlagBits::eVariableDescriptorCount;
    //                     }
    //
    //                     if (layout->declaration->declarationType == ashl::EDeclarationType::Sampler2D)
    //                     {
    //                         builders[set].AddBinding(binding, vk::DescriptorType::eSampler, stage, 1, flags);
    //                     }
    //                     else if (layout->declaration->declarationType == ashl::EDeclarationType::Block)
    //                     {
    //                         builders[set].AddBinding(binding, vk::DescriptorType::eUniformBuffer, stage);
    //                     }
    //                 }
    //             }
    //
    //             if (statement->nodeType == ashl::ENodeType::PushConstant)
    //             {
    //                 if (auto pushConstant = std::dynamic_pointer_cast<ashl::PushConstantNode>(statement))
    //                 {
    //                     pushConstantRanges.emplace_back(stage, 0, static_cast<uint32_t>(pushConstant->GetSize()));
    //                 }
    //             }
    //         }
    //
    //         auto max = builders.rbegin()->first;
    //         std::vector<vk::DescriptorSetLayout> sets{};
    //         sets.reserve(max);
    //         for (auto i = 0; i < max + 1; i++)
    //         {
    //             if (builders.contains(i))
    //             {
    //                 sets.push_back(builders[i].Build());
    //             }
    //             else
    //             {
    //                 sets.push_back(DescriptorLayoutBuilder{}.Build());
    //             }
    //         }
    //
    //         auto nextStage = task.scopeType == ashl::EScopeType::Vertex
    //                              ? vk::ShaderStageFlagBits::eFragment
    //                              : vk::ShaderStageFlagBits::eAll;
    //         auto createInfo = vk::ShaderCreateInfoEXT{
    //             {},
    //             stage,
    //             nextStage,
    //             vk::ShaderCodeTypeEXT::eBinary,
    //             spvResult.size(),
    //             spvResult.data(),
    //             "main",
    //             static_cast<unsigned int>(sets.size()),
    //             sets.data(),
    //             static_cast<unsigned int>(pushConstantRanges.size()),
    //             pushConstantRanges.data()
    //         };
    //
    //         return _graphicsModule->GetDevice().createShaderEXT(createInfo).value;
    //     }
    // }

    glslang_stage_t ShaderManager::GetLangFromScopeType(ashl::EScopeType scopeType)
    {
        switch (scopeType)
        {
        case ashl::EScopeType::Vertex:
            return glslang_stage_t::GLSLANG_STAGE_VERTEX;
        case ashl::EScopeType::Fragment:
            return glslang_stage_t::GLSLANG_STAGE_FRAGMENT;
        }

        return glslang_stage_t::GLSLANG_STAGE_VERTEX;
    }

    std::shared_future<vk::ShaderEXT> ShaderManager::StartShaderCompilation(const std::string& id,
                                                                     const std::shared_ptr<ashl::ModuleNode>& shader,
                                                                     ashl::EScopeType scopeType)
    {
        if(_vkShaders.contains(id)) return sharedFutureFromResult(_vkShaders[id]);
        if(_pendingShaders.contains(id)) return _pendingShaders[id];
        CompileTask task{id,shader,scopeType};
        return _pendingShaders.emplace(id,_compilationThread.Put([this,task]
        {
            return CompileShader(task);
        })).first->second;
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
        _compilationThread.Stop();
        glslang_finalize_process();
    }
}
