#pragma once
#include "aerox/core/memory.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"
#include "aerox/window/WindowModule.hpp"
#include "aerox/window/Window.hpp"

namespace bass
{
    class FileStream;
}

using namespace aerox;
using namespace aerox::graphics;
using namespace aerox::window;

MCLASS()
class TestModule : public Module
{
    WindowModule * _windowModule = nullptr;
    Shared<Window> _window{};
    bass::FileStream * _stream = nullptr;
    Shared<GraphicsShader> _graphicsShader{};
public:

    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(Module* module) override;
    void RegisterRequiredModules() override;
    void OnCloseRequested(Window * window);
};
