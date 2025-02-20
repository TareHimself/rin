
#include "DevModule.h"

#include <iostream>

#include "rin/core/GRuntime.h"
#include "rin/core/utils.h"
#include "rin/io/events/CloseEvent.h"
#include "rin/rhi/Image.h"
#include "rin/rhi/utils.h"

void DevModule::RegisterRequiredModules(rin::GRuntime* runtime)
{
    _ioModule = runtime->RegisterModule<rin::io::IoModule>();
    _graphicsModule = runtime->RegisterModule<rin::rhi::GraphicsModule>();
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
        _graphicsModule->Draw();
    });

    auto window =  _ioModule->CreateWindow({500,500},"Test Window",rin::io::Window::CreateOptions().Visible(true).Resizable(true));
    window->onClose->Add([this](const rin::Shared<rin::io::CloseEvent>& e)
    {
        e->window->Dispose();
        GetRuntime()->RequestExit();
    });
    auto size = _ioModule->GetTaskRunner()
    .Enqueue<std::string>([]{
       return std::string("data"); 
    })->After<size_t>([](const std::string& data){
        return data.size();
    })->After<void>([](size_t data){
        std::cout << "Done with thread calculations " << data << std::endl;
    });
    _graphicsModule->GetShaderManager()->GraphicsFromFile(rin::rhi::getBuiltInShadersPath() / "views" / "batch.slang");
    auto texture = rin::rhi::Image<>::LoadFile(rin::getResourcesPath() / "rin" / "textures" / "default.png");
    _graphicsModule->GetTextureManager()->CreateTexture(texture.GetData(),vk::Extent3D{static_cast<unsigned int>(texture.GetWidth()),static_cast<unsigned int>(texture.GetHeight()),1},rin::rhi::ImageFormat::RGBA8);
    
}

void DevModule::Shutdown(rin::GRuntime* runtime)
{
    
}

std::string DevModule::GetName()
{
    return "Dev Module";
}
