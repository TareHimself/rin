#pragma once
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include "unordered_map"
#include "aerox/graphics/WindowRenderer.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"

class WidgetWindowSurface;
class Window;

MCLASS()
class WidgetsModule : public AeroxModule
{
    std::unordered_map<WindowRenderer *,Shared<WidgetWindowSurface>> _windowSurfaces{};
    GraphicsModule * _graphicsModule = nullptr;
    DelegateListHandle _rendererCreatedHandle{};
    DelegateListHandle _rendererDestroyedHandle{};
    Shared<GraphicsShader> _batchShader{};
public:
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(AeroxModule* module) override;

    void RegisterRequiredModules() override;

    void OnRendererCreated(WindowRenderer * renderer);
    void OnRendererDestroyed(WindowRenderer * renderer);

    Shared<WidgetWindowSurface> GetSurface(Window * window) const;
    Shared<GraphicsShader> GetBatchShader() const;
};