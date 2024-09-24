#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/WindowRenderer.hpp"
#include "aerox/widgets/WidgetWindowSurface.hpp"
#include <vulkan/vulkan_funcs.hpp>
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

bool WidgetsModule::IsDependentOn(AeroxModule* module)
{
    return instanceOf<GraphicsModule>(module);
}

void WidgetsModule::RegisterRequiredModules()
{
    AeroxModule::RegisterRequiredModules();
    _graphicsModule = GRuntime::Get()->RegisterModule<GraphicsModule>();
}

void WidgetsModule::OnRendererCreated(WindowRenderer* renderer)
{
    if(!_batchShader)
    {
        _batchShader = GraphicsShader::FromFile(getResourcesPath() / "shaders" / "batch.ash");
    }
    auto surf = newShared<WidgetWindowSurface>(renderer);
    surf->Init();
    _windowSurfaces.emplace(renderer,surf);
}

void WidgetsModule::OnRendererDestroyed(WindowRenderer* renderer)
{
    _windowSurfaces[renderer]->Dispose();
    _windowSurfaces.erase(renderer);
}

Shared<WidgetWindowSurface> WidgetsModule::GetSurface(Window* window) const
{
    if(auto renderer = _graphicsModule->GetRenderer(window))
    {
        if(_windowSurfaces.contains(renderer))
        {
            return _windowSurfaces.at(renderer);
        }
    }

    return {};
}
    
Shared<GraphicsShader> WidgetsModule::GetBatchShader() const
{
    return _batchShader;
}
