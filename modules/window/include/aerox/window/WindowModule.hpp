#pragma once
#include "WindowCreateOptions.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/delegates/DelegateList.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include <SDL3/SDL_video.h>

namespace aerox::window
{
    class Window;

    MCLASS()
    class WindowModule : public Module
    {
        std::unordered_map<SDL_WindowID,Shared<Window>> _windows{};
    protected:
        
    public:
        std::string GetName() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        bool IsDependentOn(Module* module) override;
    
        Shared<Window> Create(const std::string& name,int width,int height,const WindowCreateOptions& options = {});
        
        DEFINE_DELEGATE_LIST(onWindowCreated,Window*)
        DEFINE_DELEGATE_LIST(onWindowDestroyed,Window*)
    };
}
