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
#include "rin/widgets/containers/WCPanel.hpp"
#include "rin/window/Window.hpp"
#include "rin/window/WindowModule.hpp"
#include "bass/Stream.hpp"
#include "rin/widgets/utils.hpp"
#include "rin/widgets/WidgetSurface.hpp"
#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"
#include <filesystem>
#include "rin/graphics/Image.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include "rin/widgets/containers/WCFitter.hpp"
#include "rin/widgets/content/WImage.hpp"
using namespace std::chrono_literals;


void TestClipCommand::Run(SurfaceFrame* frame)
{
    auto surf = frame->surface;
    auto cmd = frame->raw->GetCommandBuffer();
    surf->BeginMainPass(frame);

    auto size = Vec2{500.0f};
    auto displaySize = surf->GetDrawSize().Cast<float>();
    enableStencilWrite(cmd, bitshift(1), 1);
    surf->WriteStencil(frame->raw, Matrix3<float>{}, size);
    enableStencilWrite(cmd, bitshift(2), 1);
    surf->WriteStencil(frame->raw, Matrix3<float>{}.Translate(displaySize - size), size);
    enableStencilCompare(cmd, bitshift(1, 2), vk::CompareOp::eNotEqual);
}

void CoverImage::CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
{
    if (GetTextureId() < 0)
    {
        drawCommands.Add(
            SimpleBatchedDrawCommand::Builder{}
            .AddRect(
                GetContentSize(),
                transform.transform,
                GetBorderRadius(),
                GetTint()
            )
            .Finish()
        );
    }
    else
    {
        auto contentSize = GetContentSize();
        auto fitSize = WCFitter::ComputeCoverSize(contentSize, GetDesiredSize());
        auto centerDist = fitSize / 2.0f - contentSize / 2.0f;
        auto p1 = centerDist;
        auto p2 = centerDist + contentSize;
        p1 = p1 / fitSize;
        p2 = p2 / fitSize;

        drawCommands.Add(
            SimpleBatchedDrawCommand::Builder{}
            .AddTexture(
                GetTextureId(),
                GetContentSize(),
                transform.transform,
                GetBorderRadius(),
                GetTint()
                , Vec4{p1.x, p1.y, p2.x, p2.y}
            )
            .Finish()
        );
    }
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

    if (auto parent = GetParent() ? GetParent()->GetParent() : Shared<ContainerWidget>{}; parent && parent->IsHovered()
        && GetSurface())
    {
        if (auto surface = GetSurface())
        {
            auto targetLocation = surface->GetCursorPosition();
            drawCommands.Add(SimpleBatchedDrawCommand::Builder()
                             .AddRect(
                                 size,
                                 Matrix3<float>(1.0f).Translate(targetLocation).RotateDeg(angle).Translate(
                                     GetSize() * pivot * -1.0f),
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
    _window = _windowModule->Create(
        "Rin Engine Widgets",
        500,
        500,
        WindowCreateOptions{}
        .Resizable()
    );
    _window->onCloseRequested->Add(&TestModule::OnCloseRequested);

    if (const auto surface = _widgetsModule->GetSurface(_window))
    {
        _container = newShared<WCSwitcher>();
        surface->AddChild(_container);

        _window->onKey->Add([this](Window* window, const Key key, const InputState state)
        {
            if (key == Key::Return && state == InputState::Released)
            {
                tasks.Put(this, &TestModule::LoadImages);
            }
            else if ((key == Key::Left || key == Key::Right) && state == InputState::Released && _container->
                GetUsedSlots() > 0)
            {
                const auto delta = key == Key::Left ? -1 : 1;
                int currentIndex = _container->GetSelectedIndex();
                currentIndex += delta;
                const auto maxIndex = _container->GetUsedSlots() - 1;
                if (currentIndex < 0)
                {
                    currentIndex = maxIndex;
                }
                else if (currentIndex > maxIndex)
                {
                    currentIndex = 0;
                }

                _container->SetSelectedIndex(currentIndex);
            }
        });
        //panel->SetClipMode(EClipMode::Bounds);
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

void TestModule::LoadImages()
{
    if (auto files = rin::platform::selectFile("Select Images", true); !files.empty())
    {
        auto textureId = 0;
        for (auto& file : files)
        {
            std::filesystem::path imageFilePath{file};
            auto loadedTexture = Image<unsigned char>::LoadFile(imageFilePath);
            if (loadedTexture.GetElementCount() == 0) continue;
            loadedTexture.SetChannels(4);
            textureId = GraphicsModule::Get()->GetResourceManager()->CreateTexture(
                loadedTexture, ImageFormat::RGBA8, vk::Filter::eNearest, {}, true);
            auto img = newShared<CoverImage>();
            img->SetTextureId(textureId);
            img->SetPadding(Padding{20.0f});
            _container->AddChild(img);
            // auto sizer = newShared<WCSizer>();
            // sizer->AddChild(img);
            // sizer->SetOverrides(Vec2{500.0f});
            // _container->AddChild(sizer);

            // auto fitter = (newShared<FitterWidget>() + img).second;
            // fitter->SetPadding(Padding{20.0f});
            // auto slot = (_container + fitter).first;
            // fitter->SetClipMode(EClipMode::Bounds);
            // fitter->SetMode(FitMode::Cover);
        }
    }
}

void TestModule::OnCloseRequested(Window* window)
{
    std::cout << "Close Requested " << std::endl;
    GRuntime::Get()->RequestExit();
}
