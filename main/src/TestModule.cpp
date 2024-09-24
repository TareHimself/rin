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
#include "aerox/widgets/Surface.hpp"
#include "aerox/widgets/containers/Sizer.hpp"
#include "aerox/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "aerox/widgets/slots/PanelSlot.hpp"
using namespace std::chrono_literals;

void TestStencilDrawCommand::Draw(widgets::SurfaceFrame* frame)
{
    if(frame->activePass != widgets::Surface::MAIN_PASS_ID)
    {
        frame->surface->BeginMainPass(frame);
    }

    auto cmd = frame->raw->GetCommandBuffer();
    auto face = vk::StencilFaceFlagBits::eFrontAndBack;
    cmd.setStencilReference(face,1);
    cmd.setStencilWriteMask(face,0x01);
    cmd.setStencilCompareMask(face,0x0);
    cmd.setStencilOp(face,vk::StencilOp::eKeep,vk::StencilOp::eReplace,vk::StencilOp::eKeep,vk::CompareOp::eAlways);
}

Vec2<float> TestWidget::ComputeDesiredSize()
{
    return Vec2{200.0f};
}

void TestWidget::Collect(const widgets::TransformInfo& transform,
                         std::vector<Shared<widgets::DrawCommand>>& drawCommands)
{
    auto time = GRuntime::Get()->GetTimeSeconds();
    pivot = Vec2{0.5f};
    angle = sin(time) * 90.0f;
    auto size = GetDrawSize();
    drawCommands.push_back(newShared<TestStencilDrawCommand>());
    if(auto parent = GetParent(); parent && parent->IsHovered() && GetSurface())
    {
        if(auto surface = GetSurface())
        {
            auto location = surface->GetCursorPosition();
            auto t = Matrix3<float>(1.0f).Translate(location).RotateDeg(angle).Translate(GetDrawSize() * pivot * -1.0f);
            drawCommands.push_back(widgets::SimpleBatchedDrawCommand::Builder()
                               .AddRect(
                                   size,
                                   t,
                                   Vec4{
                                       abs(sin(time) * (size.x / 2.0f))
                                   }
                                   .Cast<float>(),
                                   Vec4{
                                       abs(sin(time + 1)),
                                       abs(sin(time + 2)),
                                       abs(sin(time + 3)),
                                       1.0
                                   }
                                   .Cast<float>()
                               )
                               .Finish()
    );
        }
        
    }
    else
    {
        drawCommands.push_back(widgets::SimpleBatchedDrawCommand::Builder()
                           .AddRect(
                               size,
                               transform.transform,
                               Vec4{
                                   abs(sin(time) * (size.x / 2.0f))
                               }
                               .Cast<float>(),
                               Vec4{
                                   abs(sin(time + 1)),
                                   abs(sin(time + 2)),
                                   abs(sin(time + 3)),
                                   1.0
                               }
                               .Cast<float>()
                           )
                           .Finish()
    );
    }
    
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
            //auto slot = panel->AddChild<widgets::PanelSlot, TestWidget>();
            auto sizer = (newShared<widgets::Sizer>() + newShared<TestWidget>()).second;
            sizer->SetClipMode(widgets::ClipMode::Bounds);
            auto size = 250.0f;
            sizer->SetWidthOverride(size);
            sizer->SetHeightOverride(size);
            
            auto slot = (panel + sizer).first;
            slot->minAnchor = Vec2{0.5f};
            slot->maxAnchor = Vec2{0.5f};
            slot->sizeToContent = true;
            slot->alignment = Vec2{0.0f};
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
