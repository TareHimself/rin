#pragma once
#include "rin/core/Module.hpp"
#include "rin/core/meta/MetaMacros.hpp"
#include "unordered_map"
#include "rin/core/math/Matrix3.hpp"
#include "rin/core/math/Matrix4.hpp"
#include "rin/graphics/WindowRenderer.hpp"
#include "rin/graphics/shaders/GraphicsShader.hpp"

class WidgetWindowSurface;
class Window;

struct WidgetStencilClip
{
    Matrix3<float> transform{};
    Vec2<float> size{0.0f};
};

inline int RIN_WIDGETS_MAX_STENCIL_CLIP = 12;

struct WidgetStencilBuffer
{
    Matrix4<float> projection{};
    WidgetStencilClip clips[12];
};

struct SingleStencilDrawPush
{
    Matrix4<float> projection{};
    Matrix3<float> transform{};
    Vec2<float> size{0.0f};
};

MCLASS()
class WidgetsModule : public RinModule
{
    std::unordered_map<WindowRenderer *,Shared<WidgetWindowSurface>> _windowSurfaces{};
    GraphicsModule * _graphicsModule = nullptr;
    DelegateListHandle _rendererCreatedHandle{};
    DelegateListHandle _rendererDestroyedHandle{};
    Shared<GraphicsShader> _batchShader{};
    Shared<GraphicsShader> _stencilShader{};

    
    
public:

    static WidgetsModule * Get();
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(RinModule* module) override;

    void RegisterRequiredModules() override;

    void OnRendererCreated(WindowRenderer * renderer);
    void OnRendererDestroyed(WindowRenderer * renderer);

    Shared<WidgetWindowSurface> GetSurface(Window * window) const;
    Shared<GraphicsShader> GetBatchShader() const;
    Shared<GraphicsShader> GetStencilShader() const;
    void DrawStencil(const vk::CommandBuffer& cmd,const WidgetStencilClip& pushConstants) const;
};