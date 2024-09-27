#include "TestModule.hpp"
#include <iostream>

#include "rin/audio/AudioModule.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/Module.hpp"
#include "rin/core/platform.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/widgets/WidgetContainerSlot.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/WidgetWindowSurface.hpp"
#include "rin/widgets/containers/WidgetPanel.hpp"
#include "rin/window/Window.hpp"
#include "rin/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "rin/widgets/utils.hpp"
#include "rin/widgets/containers/WidgetPanel.hpp"
#include "rin/widgets/WidgetSurface.hpp"
#include "rin/widgets/containers/WidgetSizer.hpp"
#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"
#include "rin/widgets/slots/WidgetPanelSlot.hpp"
using namespace std::chrono_literals;

void TestStencilDrawCommand::Draw(SurfaceFrame* frame)
{
    if (frame->activePass != SurfaceGlobals::MAIN_PASS_ID)
    {
        frame->surface->BeginMainPass(frame);
    }

    auto cmd = frame->raw->GetCommandBuffer();
    auto face = vk::StencilFaceFlagBits::eFrontAndBack;
    // cmd.setStencilReference(face, 1);
    // cmd.setStencilWriteMask(face, 0x1);
    // cmd.setStencilCompareMask(face, 0xFF);
    // cmd.setStencilOp(face, vk::StencilOp::eKeep, vk::StencilOp::eReplace, vk::StencilOp::eKeep, vk::CompareOp::eAlways);
    enableStencilWrite(cmd,bitmask(1),1);
    WidgetsModule::Get()->DrawStencil(cmd,{Matrix3<float>(1.0f),frame->surface->GetProjection(),Vec2{250.0f},Vec4{30.0f}});
    // auto quad = QuadInfo{-1,Vec4{0.0f},Vec4{0.0f},Vec2{250.0f},Matrix3<float>{1.0f}};
    // std::vector<QuadInfo> quads = {quad};
    // frame->surface->DrawBatches(frame,quads);
    // cmd.setStencilWriteMask(face,0x0);
    // cmd.setStencilCompareMask(face,bitmask(1));
    // cmd.setStencilOp(face, vk::StencilOp::eKeep, vk::StencilOp::eKeep, vk::StencilOp::eKeep, vk::CompareOp::eNotEqual);
    enableStencilCompare(cmd,bitmask(1),vk::CompareOp::eNotEqual);
}

Vec2<float> TestWidget::ComputeDesiredSize()
{
    return Vec2{200.0f};
}

void TestWidget::Collect(const TransformInfo& transform,
                         WidgetDrawCommands& drawCommands)
{
    auto time = GRuntime::Get()->GetTimeSeconds();
    pivot = Vec2{0.5f};
    angle = sin(time) * 90.0f;
    auto size = GetDrawSize();
    
    if (auto parent = GetParent() ? GetParent()->GetParent() : Shared<WidgetContainer>{}; parent && parent->IsHovered() && GetSurface())
    {
        if (auto surface = GetSurface())
        {
            auto targetLocation = surface->GetCursorPosition();
            drawCommands.Add(SimpleBatchedDrawCommand::Builder()
                           .AddRect(
                               size,
                               Matrix3<float>(1.0f).Translate(targetLocation).RotateDeg(angle).Translate(GetDrawSize() * pivot * -1.0f),
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
                           .Finish());
        }
    }
    else
    {
        _lastLocation = transform.transform * Vec2{0.0f};
        
        drawCommands.Add(SimpleBatchedDrawCommand::Builder()
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

bool TestModule::IsDependentOn(RinModule* module)
{
    return instanceOf<WindowModule>(module) || instanceOf<GraphicsModule>(module);
}

void TestModule::RegisterRequiredModules()
{
    RinModule::RegisterRequiredModules();
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
