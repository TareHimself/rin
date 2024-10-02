#include "rin/widgets/WidgetsModule.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/WindowRenderer.hpp"
#include "rin/widgets/WidgetWindowSurface.hpp"

WidgetsModule* WidgetsModule::Get()
{
    return GRuntime::Get()->GetModule<WidgetsModule>();
}

std::string WidgetsModule::GetName()
{
    return "Widgets Module";
}

void WidgetsModule::Startup(GRuntime* runtime)
{
    _rendererCreatedHandle = _graphicsModule->onRendererCreated->Add(this, &WidgetsModule::OnRendererCreated);
    _rendererDestroyedHandle = _graphicsModule->onRendererDestroyed->Add(this, &WidgetsModule::OnRendererDestroyed);
}

void WidgetsModule::Shutdown(GRuntime* runtime)
{
    _rendererCreatedHandle.UnBind();
    _rendererDestroyedHandle.UnBind();
    _batchShader.reset();
    _stencilShader.reset();
}

bool WidgetsModule::IsDependentOn(RinModule* module)
{
    return instanceOf<GraphicsModule>(module);
}

void WidgetsModule::RegisterRequiredModules()
{
    RinModule::RegisterRequiredModules();
    _graphicsModule = GRuntime::Get()->RegisterModule<GraphicsModule>();
}

void WidgetsModule::OnRendererCreated(WindowRenderer* renderer)
{
    if (!_batchShader)
    {
        _batchShader = GraphicsShader::FromFile(getResourcesPath() / "shaders" / "widgets" / "batch.rsl");
        _stencilShader = GraphicsShader::FromFile(getResourcesPath() / "shaders" / "widgets" / "stencil_single.rsl");
    }
    auto surf = newShared<WidgetWindowSurface>(renderer);
    surf->Init();
    _windowSurfaces.emplace(renderer, surf);
}

void WidgetsModule::OnRendererDestroyed(WindowRenderer* renderer)
{
    _windowSurfaces[renderer]->Dispose();
    _windowSurfaces.erase(renderer);
}

Shared<WidgetWindowSurface> WidgetsModule::GetSurface(Window* window) const
{
    if (auto renderer = _graphicsModule->GetRenderer(window))
    {
        if (_windowSurfaces.contains(renderer))
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

Shared<GraphicsShader> WidgetsModule::GetStencilShader() const
{
    return _stencilShader;
}

void WidgetsModule::DrawStencil(const vk::CommandBuffer& cmd, const WidgetStencilClip& pushConstants) const
{
    
}
