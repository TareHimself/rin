#include "TestModule.hpp"
#include <iostream>

#include "aerox/audio/AudioModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/platform.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/widgets/ContainerSlot.hpp"
#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/widgets/WindowSurface.hpp"
#include "aerox/widgets/containers/Panel.hpp"
#include "aerox/window/Window.hpp"  
#include "aerox/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "aerox/widgets/containers/Panel.hpp"
#include "aerox/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "aerox/widgets/slots/PanelSlot.hpp"
using namespace std::chrono_literals;

Vec2<float> TestWidget::ComputeDesiredSize()
{
    return Vec2{200.0f};
}

void TestWidget::Collect(const widgets::TransformInfo& transform,
                         std::vector<Shared<widgets::DrawCommand>>& drawCommands)
{
    auto time = GRuntime::Get()->GetTimeSeconds();
    pivot = Vec2{0.5f};
    angle = sin(time) * 200.0f;
    drawCommands.push_back(widgets::SimpleBatchedDrawCommand::Builder()
                           .AddRect(GetDrawSize(), transform.transform,Vec4<float>{static_cast<float>(abs(sin(time) * 40.0f))})
                           .Finish());
}

std::string TestModule::GetName()
{
    return "Test Module";
}

void TestModule::Startup(GRuntime* runtime)
{
    _window = _windowModule->Create("Test Window", 500, 500);
    _window->onCloseRequested->Add(&TestModule::OnCloseRequested);

    if (auto surface = _widgetsModule->GetSurface(_window.get()))
    {
        auto panel = newShared<widgets::Panel>();
        // {
        //     auto slot = panel->AddChild<widgets::PanelSlot,TestWidget>();
        //     slot->maxAnchor = {0.5f, 0.5f};
        // }
        {
            auto slot = panel->AddChild<widgets::PanelSlot,TestWidget>();
            slot->minAnchor = Vec2{0.5f};
            slot->maxAnchor = Vec2{0.5f};
            slot->size = Vec2{250.0f};
            slot->alignment = Vec2{0.5f};
        }

        surface->AddChild(panel);
    }
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
    _widgetsModule = GetRuntime()->RegisterModule<widgets::WidgetsModule>();
}

void TestModule::OnCloseRequested(Window* window)
{
    std::cout << "Close Requested " << std::endl;
    GRuntime::Get()->RequestExit();
}
