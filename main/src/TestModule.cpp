#include "TestModule.hpp"
#include <iostream>
#include "rin/audio/AudioModule.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/Module.hpp"
#include "rin/core/platform.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/WidgetWindowSurface.hpp"
#include "rin/widgets/containers/PanelWidget.hpp"
#include "rin/window/Window.hpp"
#include "rin/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "rin/widgets/utils.hpp"
#include "rin/widgets/containers/PanelWidget.hpp"
#include "rin/widgets/WidgetSurface.hpp"
#include "rin/widgets/containers/SizerWidget.hpp"
#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"
#include "rin/widgets/slots/PanelWidgetSlot.hpp"
#include <filesystem>
#include "rin/graphics/Image.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include "rin/widgets/containers/FitterWidget.hpp"
#include "rin/widgets/containers/FlexWidget.hpp"
#include "rin/widgets/content/ImageWidget.hpp"
using namespace std::chrono_literals;


void TestClipCommand::Run(SurfaceFrame* frame)
{
    auto surf = frame->surface;
    auto cmd = frame->raw->GetCommandBuffer();
    surf->BeginMainPass(frame);

    auto size = Vec2{500.0f};
    auto displaySize = surf->GetDrawSize().Cast<float>();
    enableStencilWrite(cmd,bitshift(1),1);
    surf->WriteStencil(frame->raw,Matrix3<float>{},size);
    enableStencilWrite(cmd,bitshift(2),1);
    surf->WriteStencil(frame->raw,Matrix3<float>{}.Translate(displaySize - size),size);
    enableStencilCompare(cmd,bitshift(1,2),vk::CompareOp::eNotEqual);
}

Vec2<float> TestWidget::ComputeContentSize()
{
    return Vec2{200.0f};
}

void TestWidget::Collect(const TransformInfo& transform,
                         WidgetDrawCommands& drawCommands)
{
    auto time = GRuntime::Get()->GetTimeSeconds();
    pivot = Vec2{0.5f};
    angle = sin(time) * 90.0f;
    auto size = GetSize();
    
    if (auto parent = GetParent() ? GetParent()->GetParent() : Shared<ContainerWidget>{}; parent && parent->IsHovered() && GetSurface())
    {
        if (auto surface = GetSurface())
        {
            auto targetLocation = surface->GetCursorPosition();
            drawCommands.Add(SimpleBatchedDrawCommand::Builder()
                           .AddRect(
                               size,
                               Matrix3<float>(1.0f).Translate(targetLocation).RotateDeg(angle).Translate(GetSize() * pivot * -1.0f),
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
    _window = _windowModule->Create("Rin Engine", 500, 500);
    _window->onCloseRequested->Add(&TestModule::OnCloseRequested);
    
    if (auto surface = _widgetsModule->GetSurface(_window.get()))
    {
        auto mainContainer = newShared<FlexWidget>();
        {
            auto textureId = 0;
            
            if(auto file = rin::platform::selectFile("Select An Image",false); !file.empty())
            {
                std::filesystem::path imageFilePath{file.front()};
                auto loadedTexture = Image<unsigned char>::LoadFile(imageFilePath);
                loadedTexture.SetChannels(4);
                textureId = GraphicsModule::Get()->GetResourceManager()->CreateTexture(loadedTexture,ImageFormat::RGBA8,vk::Filter::eNearest,{},true);
            }
            for(auto i = 0; i < 3; i++)
            {
                auto img = newShared<ImageWidget>();
                img->SetTextureId(textureId);
                auto fitter = (newShared<FitterWidget>() + img).second;
                fitter->SetPadding(Padding{20.0f});
                auto slot = (mainContainer + fitter).first;
                fitter->SetClipMode(EClipMode::Bounds);
                fitter->SetMode(FitMode::Cover);
            }
        }
        
        //panel->SetClipMode(EClipMode::Bounds);
        surface->AddChild(mainContainer);
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
