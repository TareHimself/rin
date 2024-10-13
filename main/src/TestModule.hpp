#pragma once
#include "rin/core/memory.hpp"
#include "rin/core/Module.hpp"
#include "rin/core/meta/MetaMacros.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/shaders/GraphicsShader.hpp"
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/containers/FlexWidget.hpp"
#include "rin/widgets/containers/ScrollableWidget.hpp"
#include "rin/widgets/content/ImageWidget.hpp"
#include "rin/widgets/graphics/WidgetCustomDrawCommand.hpp"
#include "rin/window/WindowModule.hpp"
#include "rin/window/Window.hpp"

namespace bass
{
    class FileStream;
}

class TestClipCommand : public WidgetCustomDrawCommand
{
    void Run(SurfaceFrame* frame) override;
};


class CoverImage : public ImageWidget
{
    void CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;
};

// class TextTestWidget : public Widget
// {
// protected:
//     SDFContainer _sdfInfo{};
//     TextTestWidget();
//     Vec2<float> ComputeContentSize() override;
//     void CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;
//
// public:
//     void Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;
// };

class TestWidget : public Widget
{
    Vec2<float> _lastLocation{0.0f};

protected:
    Vec2<float> ComputeContentSize() override;

public:
    void Collect(const TransformInfo& transform,
                 WidgetDrawCommands& drawCommands) override;
};

MCLASS()

class TestModule : public RinModule
{
    WindowModule* _windowModule = nullptr;
    WidgetsModule* _widgetsModule = nullptr;
    Shared<Window> _window{};
    bass::FileStream* _stream = nullptr;
    Shared<ScrollableWidget> _container{};

public:
    BackgroundThread<void> tasks{};
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(RinModule* module) override;
    void RegisterRequiredModules() override;
    void LoadImages();
    static void OnCloseRequested(Window* window);
};
