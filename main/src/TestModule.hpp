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

using namespace aerox;
using namespace aerox::graphics;
using namespace aerox::window;

class TestStencilDrawCommand : public widgets::CustomDrawCommand
{
    void Draw(widgets::SurfaceFrame* frame) override;
};
class TestWidget : public widgets::Widget
{
protected:
    Vec2<float> ComputeDesiredSize() override;

public:
    void Collect(const widgets::TransformInfo& transform,
        std::vector<Shared<widgets::DrawCommand>>& drawCommands) override;
};
MCLASS()
class TestModule : public Module
{
    WindowModule * _windowModule = nullptr;
    widgets::WidgetsModule * _widgetsModule = nullptr;
    Shared<Window> _window{};
    bass::FileStream * _stream = nullptr;
public:

    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(Module* module) override;
    void RegisterRequiredModules() override;
    static void OnCloseRequested(Window * window);
};
