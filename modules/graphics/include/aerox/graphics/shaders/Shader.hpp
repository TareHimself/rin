#pragma once
#include "ShaderManager.hpp"
#include "ashl/nodes.hpp"
namespace aerox::graphics
{
    class Shader : public Disposable
    {
        
    protected:
        std::string _cachedId{};
        ShaderManager * _shaderManager{};

        ShaderManager * GetManager() const;
    public:

        struct Texture
        {
            std::string name;
            uint32_t set;
            uint32_t binding;
            uint32_t count;
            Texture(const std::string& inName,const uint32_t& inSet,const uint32_t& inBinding,const uint32_t& inCount);
        };

        struct Buffer
        {
            std::string name;
            uint32_t set;
            uint32_t binding;
            uint32_t size;
            uint32_t count;
            Buffer(const std::string& inName,const uint32_t& inSet,const uint32_t& inBinding,const uint32_t& inSize,const uint32_t& inCount);
        };

        struct PushConstant
        {
            // public string Name;
            // public int Size;
            // public VkShaderStageFlags Stages;
            std::string name;
            uint64_t size;
            vk::ShaderStageFlags stages;
            PushConstant(const std::string& inName,const uint64_t& inSize,const vk::ShaderStageFlags& inStages);
        };

        static vk::ShaderStageFlags ScopeTypeToStageFlags(const ashl::EScopeType& scopeType);

        Shared<ashl::ModuleNode> module{};

        ashl::EScopeType scopeType{};

        std::unordered_map<std::string,Texture> textures{};

        std::unordered_map<std::string,Buffer> buffers{};

        std::unordered_map<std::string,PushConstant> pushConstants{};

        explicit Shader(ShaderManager * manager);
        virtual bool Bind(const vk::CommandBuffer& cmd,bool wait = false) = 0;

        // Shader(const Shared<ashl::ModuleNode>& inModule,ashl::EScopeType inScopeType);
        // Shader(const Shared<ashl::ModuleNode>& inModule,ashl::EScopeType inScopeType,const std::string& inId);
    };
}
