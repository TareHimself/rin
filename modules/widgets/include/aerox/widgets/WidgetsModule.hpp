#pragma once
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include "unordered_map"
#include "aerox/graphics/WindowRenderer.hpp"

namespace aerox::widgets
{
    class WindowSurface;
}

namespace aerox::widgets {
    class Window;

    MCLASS()
    class WidgetsModule : public Module
    {
        std::unordered_map<graphics::WindowRenderer *,Shared<WindowSurface>> _windowSurfaces{};
        graphics::GraphicsModule * _graphicsModule = nullptr;
        DelegateListHandle _rendererCreatedHandle{};
        DelegateListHandle _rendererDestroyedHandle{};
    public:
        std::string GetName() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        bool IsDependentOn(Module* module) override;

        void RegisterRequiredModules() override;

        void OnRendererCreated(graphics::WindowRenderer * renderer);
        void OnRendererDestroyed(graphics::WindowRenderer * renderer);
    };
}
