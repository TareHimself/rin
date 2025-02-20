#include "rin/gui/GUIModule.h"

#include "rin/core/GRuntime.h"
namespace rin::gui
{
    void GUIModule::Startup(GRuntime* runtime)
    {
        //_graphicsModule->onRendererCreated
    }
    void GUIModule::Shutdown(GRuntime* runtime)
    {
    }
    void GUIModule::RegisterRequiredModules(GRuntime* runtime)
    {
        _graphicsModule = runtime->RegisterModule<rhi::GraphicsModule>();
    }
    std::string GUIModule::GetName()
    {
        return "GUI Module";
    }
    bool GUIModule::IsDependentOn(Module* module)
    {
        return module == _graphicsModule || _graphicsModule->IsDependentOn(module);
    }
}
