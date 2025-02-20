#pragma once
#include "rin/core/Module.h"
#include "rin/rhi/GraphicsModule.h"
namespace rin::gui
{
    class GUIModule : public Module
    {
        rhi::GraphicsModule * _graphicsModule = nullptr;
    protected:
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        void RegisterRequiredModules(GRuntime* runtime) override;
    public:
        std::string GetName() override;
        bool IsDependentOn(Module* module) override;

    };
}
