
#include "DevModule.h"

#include <iostream>

#include "rin/core/GRuntime.h"
#include "rin/graphics/utils.h"

void DevModule::RegisterRequiredModules(rin::GRuntime* runtime)
{
    _ioModule = runtime->RegisterModule<rin::io::IoModule>();
    _graphicsModule = runtime->RegisterModule<rin::graphics::GraphicsModule>();
}

bool DevModule::IsDependentOn(Module* module)
{
    return module == _ioModule || module == _graphicsModule;
}

void DevModule::Startup(rin::GRuntime* runtime)
{
    runtime->onTick->Add([this](double delta)
    {
        _ioModule->Tick(delta);
    });

    auto window =  _ioModule->CreateWindow({500,500},"Test Window",rin::io::IWindow::CreateOptions().Visible(true).Resizable(true));
    window->onClose->Add([this](const rin::Shared<rin::io::IWindow::CloseEvent>& e)
    {
        e->window->Dispose();
        GetRuntime()->RequestExit();
    });

    _graphicsModule->GetShaderManager()->GraphicsFromFile(rin::graphics::getBuiltInShadersPath() / "views" / "batch.slang");
}

void DevModule::Shutdown(rin::GRuntime* runtime)
{
    
}

std::string DevModule::GetName()
{
    return "Dev Module";
}
