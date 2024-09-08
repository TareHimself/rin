#pragma once
#include <mutex>
#include <typeindex>
#include <unordered_map>
#include "memory.hpp"
#include "Module.hpp"
#include "delegates/DelegateList.hpp"
namespace aerox
{
    class GRuntime
    {
    private:
        static Unique<GRuntime> _runtime;
        static std::mutex _mutex;

        std::unordered_map<size_t,Shared<Module>> _modules{};
        std::vector<Shared<Module>> _moduleList{};
        bool _exitRequested = false;
        
    protected:
        void StartupModules();
        void ShutdownModules();
        void Loop();
    public:

    
        static GRuntime * Get();

        // Creates the sp
        template<typename T>
        std::enable_if_t<std::is_base_of_v<Module, T>,T*> RegisterModule();

        template<typename T>
        std::enable_if_t<std::is_base_of_v<Module, T>,T*> GetModule();

        bool WillExit() const;
    
        void RequestExit();
    
        void Run();

        DEFINE_DELEGATE_LIST(onTick,double)
    };
    
    template <typename T>
    std::enable_if_t<std::is_base_of_v<Module, T>, T*> GRuntime::RegisterModule()
    {
        auto typeIndex = typeid(T).hash_code();

        if(_modules.contains(typeIndex)) return dynamic_cast<std::enable_if_t<std::is_base_of_v<Module, T>, T*>>(_modules[typeIndex].get());
        
        auto module = newShared<T>();
        
        _modules.insert({typeIndex,std::dynamic_pointer_cast<Module>(module)});
        
        return module.get();
    }

    template <typename T>
    std::enable_if_t<std::is_base_of_v<Module, T>, T*> GRuntime::GetModule()
    {
        static_assert(std::is_base_of_v<Module, T>, "T must inherit from Module");
        const auto typeIdx = typeid(T).hash_code();
        if(!_modules.contains(typeIdx)) return nullptr;
        return dynamic_cast<std::enable_if_t<std::is_base_of_v<Module, T>, T*>>(_modules[typeIdx].get());
    }
}
