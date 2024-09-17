#pragma once
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include "unordered_map"
#include "aerox/graphics/WindowRenderer.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"

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
        Shared<graphics::GraphicsShader> _batchShader{};
    public:
        std::string GetName() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        bool IsDependentOn(Module* module) override;

        void RegisterRequiredModules() override;

        void OnRendererCreated(graphics::WindowRenderer * renderer);
        void OnRendererDestroyed(graphics::WindowRenderer * renderer);

        Shared<WindowSurface> GetSurface(window::Window * window) const;
        Shared<graphics::GraphicsShader> GetBatchShader() const;
    };
}
