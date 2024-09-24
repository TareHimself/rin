#pragma once
#include "aerox/core/memory.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"
#include "aerox/widgets/Widget.hpp"
#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/widgets/graphics/CustomDrawCommand.hpp"
#include "aerox/window/WindowModule.hpp"
#include "aerox/window/Window.hpp"

namespace bass
{
    class FileStream;
}

class TestStencilDrawCommand : public CustomDrawCommand
{
    void Draw(SurfaceFrame* frame) override;
};
class TestWidget : public Widget
{
    Vec2<float> _lastLocation{0.0f};
protected:
    Vec2<float> ComputeDesiredSize() override;

public:
    void Collect(const TransformInfo& transform,
        std::vector<Shared<DrawCommand>>& drawCommands) override;
};
MCLASS()
class TestModule : public AeroxModule
{
    WindowModule * _windowModule = nullptr;
    WidgetsModule * _widgetsModule = nullptr;
    Shared<Window> _window{};
    bass::FileStream * _stream = nullptr;
public:

    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(AeroxModule* module) override;
    void RegisterRequiredModules() override;
    static void OnCloseRequested(Window * window);
};
