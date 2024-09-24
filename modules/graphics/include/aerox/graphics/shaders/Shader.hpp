#pragma once
#include "CompiledShader.hpp"
#include "ShaderManager.hpp"
#include "ShaderResource.hpp"
#include "ashl/nodes.hpp"
class Shader : public Disposable
{
        
protected:
    std::string _cachedId{};
    ShaderManager * _shaderManager{};

    ShaderManager * GetManager() const;
public:

        
        
    struct PushConstant
    {
        std::string name;
        uint64_t size;
        vk::ShaderStageFlags stages;
        PushConstant(const std::string& inName,const uint64_t& inSize,const vk::ShaderStageFlags& inStages);
    };

    void ComputeResources(const std::vector<std::pair<vk::ShaderStageFlags,std::shared_ptr<ashl::ModuleNode>>>& shaders);

    static vk::ShaderStageFlags ScopeTypeToStageFlags(const ashl::EScopeType& scopeType);

    std::unordered_map<std::string,ShaderResource> resources{};
    std::unordered_map<std::string,PushConstant> pushConstants{};

    explicit Shader(ShaderManager * manager);
    virtual bool Bind(const vk::CommandBuffer& cmd,bool wait = false) = 0;

    virtual CompiledShader Compile(ShaderManager * manager) = 0;
    std::map<uint32_t,vk::DescriptorSetLayout> ComputeDescriptorSetLayouts() const;
    std::vector<vk::PushConstantRange> ComputePushConstantRanges() const;
    virtual std::map<uint32_t,vk::DescriptorSetLayout> GetDescriptorSetLayouts() = 0;
    virtual vk::PipelineLayout GetPipelineLayout() = 0;

    // Shader(const Shared<ashl::ModuleNode>& inModule,ashl::EScopeType inScopeType);
    // Shader(const Shared<ashl::ModuleNode>& inModule,ashl::EScopeType inScopeType,const std::string& inId);
};
