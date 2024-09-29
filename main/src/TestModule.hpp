#pragma once
#include "rin/core/memory.hpp"
#include "rin/core/Module.hpp"
#include "rin/core/meta/MetaMacros.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/shaders/GraphicsShader.hpp"
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/graphics/WidgetCustomDrawCommand.hpp"
#include "rin/window/WindowModule.hpp"
#include "rin/window/Window.hpp"

namespace bass
{
    class FileStream;
}


class TestWidget : public Widget
{
    Vec2<float> _lastLocation{0.0f};
protected:
    Vec2<float> ComputeDesiredSize() override;

public:
    void Collect(const TransformInfo& transform,
                 WidgetDrawCommands& drawCommands) override;
};
MCLASS()
class TestModule : public RinModule
{
    WindowModule * _windowModule = nullptr;
    WidgetsModule * _widgetsModule = nullptr;
    Shared<Window> _window{};
    bass::FileStream * _stream = nullptr;
public:

    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(RinModule* module) override;
    void RegisterRequiredModules() override;
    static void OnCloseRequested(Window * window);
};
