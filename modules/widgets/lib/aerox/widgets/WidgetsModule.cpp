#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/WindowRenderer.hpp"
#include "aerox/widgets/WindowSurface.hpp"

namespace aerox::widgets {
    std::string WidgetsModule::GetName()
    {
        return "Widgets Module";
    }

    void WidgetsModule::Startup(GRuntime* runtime)
    {
        _rendererCreatedHandle = _graphicsModule->onRendererCreated->Add(this,&WidgetsModule::OnRendererCreated);
        _rendererDestroyedHandle = _graphicsModule->onRendererDestroyed->Add(this,&WidgetsModule::OnRendererDestroyed);
    }

    void WidgetsModule::Shutdown(GRuntime* runtime)
    {
        _rendererCreatedHandle.UnBind();
        _rendererDestroyedHandle.UnBind();
    }

    bool WidgetsModule::IsDependentOn(Module* module)
    {
        return instanceOf<graphics::GraphicsModule>(module);
    }

    void WidgetsModule::RegisterRequiredModules()
    {
        Module::RegisterRequiredModules();
        _graphicsModule = GRuntime::Get()->RegisterModule<graphics::GraphicsModule>();
    }

    void WidgetsModule::OnRendererCreated(graphics::WindowRenderer* renderer)
    {
        auto surf = newShared<WindowSurface>(renderer);
        surf->Init();
        _windowSurfaces.emplace(renderer,surf);
    }

    void WidgetsModule::OnRendererDestroyed(graphics::WindowRenderer* renderer)
    {
        _windowSurfaces[renderer]->Dispose();
        _windowSurfaces.erase(renderer);
    }
}
