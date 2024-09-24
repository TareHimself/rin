#include "TestModule.hpp"
#include <iostream>

#include "aerox/audio/AudioModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/core/platform.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/widgets/WidgetContainerSlot.hpp"
#include "aerox/widgets/WidgetsModule.hpp"
#include "aerox/widgets/WidgetWindowSurface.hpp"
#include "aerox/widgets/containers/WidgetPanel.hpp"
#include "aerox/window/Window.hpp"
#include "aerox/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "aerox/widgets/containers/WidgetPanel.hpp"
#include "aerox/widgets/WidgetSurface.hpp"
#include "aerox/widgets/containers/WidgetSizer.hpp"
#include "aerox/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "aerox/widgets/slots/WidgetPanelSlot.hpp"
using namespace std::chrono_literals;

void TestStencilDrawCommand::Draw(SurfaceFrame* frame)
{
    if (frame->activePass != SurfaceGlobals::MAIN_PASS_ID)
    {
        frame->surface->BeginMainPass(frame);
    }

    auto cmd = frame->raw->GetCommandBuffer();
    auto face = vk::StencilFaceFlagBits::eFrontAndBack;
    cmd.setStencilReference(face, 1);
    cmd.setStencilWriteMask(face, 0x01);
    cmd.setStencilCompareMask(face, 0x0);
    cmd.setStencilOp(face, vk::StencilOp::eKeep, vk::StencilOp::eReplace, vk::StencilOp::eKeep, vk::CompareOp::eAlways);
}

Vec2<float> TestWidget::ComputeDesiredSize()
{
    return Vec2{200.0f};
}

void TestWidget::Collect(const TransformInfo& transform,
                         std::vector<Shared<DrawCommand>>& drawCommands)
{
    auto time = GRuntime::Get()->GetTimeSeconds();
    pivot = Vec2{0.5f};
    angle = sin(time) * 90.0f;
    auto size = GetDrawSize();
    
    drawCommands.push_back(newShared<TestStencilDrawCommand>());

    auto targetLocation = transform.transform * GetRelativeOffset();
    auto currentLocation = _lastLocation;
    
    if (auto parent = GetParent() ? GetParent()->GetParent() : Shared<WidgetContainer>{}; parent && parent->IsHovered() && GetSurface())
    {
        if (auto surface = GetSurface())
        {
            targetLocation = surface->GetCursorPosition();
            auto t = 
        }
    }
    else
    {
        drawCommands.push_back(SimpleBatchedDrawCommand::Builder()
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

    _lastLocation = _lastLocation.InterpolateTo(targetLocation,);
    Matrix3<float>(1.0f).Translate().RotateDeg(angle).Translate(GetDrawSize() * pivot * -1.0f);
    drawCommands.push_back(SimpleBatchedDrawCommand::Builder()
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
        auto panel = newShared<WidgetPanel>();
        {
            auto sizer = (newShared<WidgetSizer>() + newShared<TestWidget>()).second;
            sizer->SetClipMode(EClipMode::Bounds);
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

bool TestModule::IsDependentOn(AeroxModule* module)
{
    return instanceOf<WindowModule>(module) || instanceOf<GraphicsModule>(module);
}

void TestModule::RegisterRequiredModules()
{
    AeroxModule::RegisterRequiredModules();
    GetRuntime()->RegisterModule<AudioModule>();
    GetRuntime()->RegisterModule<GraphicsModule>();
    _windowModule = GetRuntime()->RegisterModule<WindowModule>();
    _widgetsModule = GetRuntime()->RegisterModule<WidgetsModule>();
}

void TestModule::OnCloseRequested(Window* window)
{
    std::cout << "Close Requested " << std::endl;
    GRuntime::Get()->RequestExit();
}
