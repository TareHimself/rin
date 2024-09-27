#pragma once
#include "WindowCreateOptions.hpp"
#include "rin/core/Module.hpp"
#include "rin/core/delegates/DelegateList.hpp"
#include "rin/core/meta/MetaMacros.hpp"
#include <SDL3/SDL_video.h>

class Window;

MCLASS()
class WindowModule : public RinModule
{
    std::unordered_map<SDL_WindowID,Shared<Window>> _windows{};
protected:
        
public:
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(RinModule* module) override;
    
    Shared<Window> Create(const std::string& name,int width,int height,const WindowCreateOptions& options = {});
        
    DEFINE_DELEGATE_LIST(onWindowCreated,Window*)
    DEFINE_DELEGATE_LIST(onWindowDestroyed,Window*)
};
