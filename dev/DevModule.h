#pragma once
#include "rin/core/UserModule.h"
#include "rin/rhi/GraphicsModule.h"
#include "rin/io/IOModule.h"
#include "rin/io/TaskRunner.h"


class DevModule : public rin::UserModule
{
    rin::io::IoModule * _ioModule{};
    rin::rhi::GraphicsModule * _graphicsModule{};
    rin::io::TaskRunner _testTaskRunner{};
    void RegisterRequiredModules(rin::GRuntime* runtime) override;
    bool IsDependentOn(Module* module) override;

    void Startup(rin::GRuntime* runtime) override;

    void Shutdown(rin::GRuntime* runtime) override;
    

    std::string GetName() override;

protected:
};
