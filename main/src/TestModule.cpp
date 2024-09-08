#include "TestModule.hpp"
#include <iostream>

#include "aerox/audio/AudioModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/widgets/containers/Panel.hpp"
#include "aerox/window/Window.hpp"
#include "aerox/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "aerox/widgets/containers/Panel.hpp"
using namespace std::chrono_literals;
std::string TestModule::GetName()
{
    return "Test Module";
}

void TestModule::Startup(GRuntime* runtime)
{
    _window = _windowModule->Create("Test Window",500,500);
    _window->onCloseRequested->Add(this->GetSharedDynamic<TestModule>(),&TestModule::OnCloseRequested);
    _graphicsShader = GraphicsShader::FromFile(R"(C:\Github\aeroxCppSwitch\modules\widgets\resources\shaders\batch.ash)");
    // if(const auto sample = bass::createFileStream(R"(C:\Users\Taree\Downloads\Tracks\Sunny - Yorushika.mp3)",0,bass::CreateFlag::SampleFloat | bass::CreateFlag::SampleMono); sample->Play())
    // {
    //     sample->SetAttribute(bass::Attribute::Volume,0.6f);
    //     _stream = sample;
    //     // while(true)
    //     // {
    //     //     std::cout << "Position:" << sample->BytesToSeconds(sample->GetPosition(bass::Position::Byte)) << "     " << "Volume: " << sample->GetAttribute(bass::Attribute::Volume) << std::endl;
    //     //     std::this_thread::sleep_for(100ms);
    //     // }
    // }
}

void TestModule::Shutdown(GRuntime* runtime)
{
    delete _stream;
}

bool TestModule::IsDependentOn(Module* module)
{
    return instanceOf<WindowModule>(module) || instanceOf<GraphicsModule>(module);
}

void TestModule::RegisterRequiredModules()
{
    Module::RegisterRequiredModules();
    GRuntime::Get()->RegisterModule<audio::AudioModule>();
    _windowModule = GRuntime::Get()->RegisterModule<WindowModule>();
    GetRuntime()->RegisterModule<GraphicsModule>();
    GetRuntime()->RegisterModule<widgets::WidgetsModule>();
}

void TestModule::OnCloseRequested(Window* window)
{
    std::cout << "Close Requested " << std::endl;
    GRuntime::Get()->RequestExit();
}
