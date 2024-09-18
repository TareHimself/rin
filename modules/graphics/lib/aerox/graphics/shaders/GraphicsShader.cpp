#include "aerox/graphics/shaders/GraphicsShader.hpp"
#include <ranges>
#include <ashl/utils.hpp>
#include <iostream>
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"
#include "aerox/graphics/shaders/ShaderCompileError.hpp"
#include "ashl/tokenizer.hpp"
#include "ashl/parser.hpp"

namespace aerox::graphics
{
    std::unordered_map<std::string, Shared<GraphicsShader>> GraphicsShader::_shaders = {};

    GraphicsShader::GraphicsShader(ShaderManager* manager, const Shared<ashl::ModuleNode>& inVertex,
                                   const Shared<ashl::ModuleNode>& inFragment) : Shader(manager)
    {
        _vertexShader = inVertex;
        _fragmentShader = inFragment;
        _vertexShaderId = std::to_string(_vertexShader->ComputeHash());
        _fragmentShaderId = std::to_string(_fragmentShader->ComputeHash());
    }

    bool GraphicsShader::Bind(const vk::CommandBuffer& cmd, bool wait)
    {
        try
        {
            using namespace std::chrono_literals;
            if (wait)
            {
                _compiledShader.wait();
            }
            else if (_compiledShader.wait_for(0ms) != std::future_status::ready)
            {
                return false;
            }
            auto data = _compiledShader.get();
            std::vector<vk::ShaderEXT> shaders = {data.shaders[0].second,data.shaders[1].second};
            std::vector<vk::ShaderStageFlagBits> stages = {
                data.shaders[0].first, data.shaders[1].first
            };
        
            {
                auto stage = vk::ShaderStageFlagBits::eGeometry;
                cmd.bindShadersEXT(1, &stage, nullptr, GraphicsModule::dispatchLoader);
            }

            cmd.bindShadersEXT(stages, shaders, GraphicsModule::dispatchLoader);
            return true;
        }
        catch (ShaderCompileError& e)
        {
            std::cerr << "Shader Error:\n" << e.what() << std::endl;
            return false;
        }
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

        const auto fullPath = std::filesystem::absolute(path).string();
        if (_shaders.contains(fullPath)) return _shaders[fullPath];
        auto manager = GraphicsModule::Get()->GetShaderManager();
        auto nodes = tokenize(fullPath);
        const auto ast = parse(nodes);
        auto vertex = extractScope(ast, ashl::EScopeType::Vertex);
        auto fragment = extractScope(ast, ashl::EScopeType::Fragment);
        resolveIncludes(vertex);
        resolveIncludes(fragment);
        resolveReferences(vertex);
        resolveReferences(fragment);
        auto shader = newShared<GraphicsShader>(manager, vertex, fragment);
        shader->Init();
        return shader;
    }

    void GraphicsShader::Init()
    {
        const auto thisShader = this->GetSharedDynamic<Shader>();


        ComputeResources({
            {vk::ShaderStageFlagBits::eVertex, _vertexShader}, {vk::ShaderStageFlagBits::eFragment, _fragmentShader}
        });
        
        _compiledShader = GetManager()->StartShaderCompilation(thisShader);
        
    }

    CompiledShader GraphicsShader::Compile(ShaderManager* manager)
    {
        auto vertexSpirv = manager->CompileAstToSpirv(_vertexShaderId, vk::ShaderStageFlagBits::eVertex, _vertexShader);
        auto fragmentSpirv = manager->CompileAstToSpirv(_fragmentShaderId, vk::ShaderStageFlagBits::eFragment,
                                                        _fragmentShader);


        DescriptorLayoutBuilder layoutBuilder{};
        std::vector<vk::PushConstantRange> pushConstantRanges{};
        std::map<uint32_t, DescriptorLayoutBuilder> builders{};
        CompiledShader shader{};

        for (auto& item : resources | std::views::values)
        {
            if (!builders.contains(item.set))
                builders.emplace(item.set,
                                 DescriptorLayoutBuilder{});

            builders.at(item.set).AddBinding(item.binding, item.type, item.stages, item.count, item.bindingFlags);
        }

        for (auto& item : pushConstants | std::views::values)
        {
            pushConstantRanges.emplace_back(item.stages, 0, static_cast<uint32_t>(item.size));
        }

        auto max = builders.empty() ? 0 : builders.rbegin()->first;
        std::vector<vk::DescriptorSetLayout> layouts{};
        layouts.reserve(max);
        for (auto i = 0; i < max + 1; i++)
        {
            auto newLayout = builders.contains(i) ? builders[i].Build() : DescriptorLayoutBuilder{}.Build();
            shader.descriptorLayouts.emplace(i, newLayout);
            layouts.push_back(newLayout);
        }


        auto device = manager->GetGraphicsModule()->GetDevice();
        {
            vk::PipelineLayoutCreateInfo createInfo{};
            createInfo.setSetLayouts(layouts);
            createInfo.setPushConstantRanges(pushConstantRanges);
        
            shader.pipelineLayout = device.createPipelineLayout(createInfo);
        }
        
        {
            auto createInfo = vk::ShaderCreateInfoEXT{
                {},
                vk::ShaderStageFlagBits::eVertex,
                vk::ShaderStageFlagBits::eFragment,
                vk::ShaderCodeTypeEXT::eSpirv,
                vertexSpirv.size() * sizeof(uint32_t),
                vertexSpirv.data(),
                "main",
                static_cast<unsigned int>(layouts.size()),
                layouts.data(),
                static_cast<unsigned int>(pushConstantRanges.size()),
                pushConstantRanges.data()
            };

            auto result = device.createShaderEXT(createInfo, nullptr, GraphicsModule::dispatchLoader);
            if (result.result != vk::Result::eSuccess)
            {
                throw std::runtime_error("Failed to compile shader");
            }

            shader.shaders.emplace_back(vk::ShaderStageFlagBits::eVertex, result.value);
        }

        {
            auto createInfo = vk::ShaderCreateInfoEXT{
                {},
                vk::ShaderStageFlagBits::eFragment,
                static_cast<vk::ShaderStageFlagBits>(0),
                vk::ShaderCodeTypeEXT::eSpirv,
                fragmentSpirv.size() * sizeof(uint32_t),
                fragmentSpirv.data(),
                "main",
                static_cast<unsigned int>(layouts.size()),
                layouts.data(),
                static_cast<unsigned int>(pushConstantRanges.size()),
                pushConstantRanges.data()
            };
            auto result = device.createShaderEXT(createInfo, nullptr, GraphicsModule::dispatchLoader);
            if (result.result != vk::Result::eSuccess)
            {
                throw std::runtime_error("Failed to compile shader");
            }

            shader.shaders.emplace_back(vk::ShaderStageFlagBits::eFragment, result.value);
        }


        manager->onDispose->Add([device,shader,layouts]
        {
           for (auto &descriptorSetLayout : layouts)
           {
               device.destroyDescriptorSetLayout(descriptorSetLayout);
           }

            device.destroyPipelineLayout(shader.pipelineLayout);
            
            for (const auto& shaderObject : shader.shaders | std::views::values)
            {
                device.destroyShaderEXT(shaderObject,nullptr,GraphicsModule::dispatchLoader);
            } 
        });
        return shader;
    }

    std::map<uint32_t, vk::DescriptorSetLayout> GraphicsShader::GetDescriptorSetLayouts()
    {
        return _compiledShader.get().descriptorLayouts;
    }

    vk::PipelineLayout GraphicsShader::GetPipelineLayout()
    {
        return _compiledShader.get().pipelineLayout;
    }
}
