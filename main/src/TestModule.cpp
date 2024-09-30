#include "TestModule.hpp"
#include <iostream>
#include <CImg/CImg.h>
// #define STB_IMAGE_WRITE_IMPLEMENTATION
// #define STB_IMAGE_IMPLEMENTATION
// #include <stb/stb_image_write.h>
// #include <stb/stb_image.h>
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
#include <filesystem>
#include "rin/graphics/Image.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include "rin/widgets/containers/WidgetFitter.hpp"
#include "rin/widgets/content/ImageWidget.hpp"
using namespace std::chrono_literals;
using namespace cimg_library;

template<typename T>
std::vector<T> toStbFormat(const CImg<T>& img)
{
    auto width = img.width();
    auto height = img.height();
    auto channels = img.spectrum();
    std::vector<T> result{};
    result.resize(width * height * channels);

    for(auto y = 0; y < height; y++)
    {
        for(auto x = 0; x < width; x++)
        {
            for(auto c = 0; c < channels; c++)
            {
                result.push_back(img(x,y,c));
            }
        }
    }

    return result;
}

template<typename T>
CImg<T> FromStb(T * ptr,int width,int height,int channels)
{
    CImg<T> img{static_cast<unsigned int>(width),static_cast<unsigned int>(height),sizeof(T),static_cast<unsigned int>(channels)};
    
    for(auto y = 0; y < height; y++)
    {
        for(auto x = 0; x < width; x++)
        {
            for(auto c = 0; c < channels; c++)
            {
                auto i = y * (width * channels) + (x * channels + c);
                img(x,y,c) = ptr[i];
            }
        }
    }

    return img;
}
//
// CImg<unsigned char> loadImage(const std::filesystem::path& path)
// {
//     int width;
//     int height;
//     int channels;
//     
//     auto ptr = stbi_load(path.string().c_str(),&width,&height,&channels,0);
//
//     auto img = FromStb<unsigned char>(ptr,width,height,channels);
//     img.display();
//     stbi_write_png("test1.png",width,height,channels,ptr,width * channels);
//
//     stbi_image_free(ptr);
//
//     return img;
// }
//
// CImg<unsigned char> saveImage(const CImg<unsigned char>& img,const std::filesystem::path& path)
// {
//     auto width = img.width();
//     auto height = img.height();
//     auto channels = img.spectrum();
//     auto data = toStbFormat(img);
//     stbi_write_jpg(path.string().c_str(),width,height,channels,data.data(),100);
//     //stbi_write_png(path.string().c_str(),img.width(),img.height(),img.depth(),img.data(),sizeof(unsigned char));
//     return img;
// }

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
    _window = _windowModule->Create("Rin Engine", 500, 500);
    _window->onCloseRequested->Add(&TestModule::OnCloseRequested);
    
    if (auto surface = _widgetsModule->GetSurface(_window.get()))
    {
        auto panel = newShared<WidgetPanel>();
        {
            auto textureId = 0;
            
            if(auto file = rin::platform::selectFile("Select An Image",false); !file.empty())
            {
                std::filesystem::path imageFilePath{file.front()};
                auto loadedTexture = Image<unsigned char>::LoadFile(imageFilePath);
                loadedTexture.SetChannels(4);
                textureId = GraphicsModule::Get()->GetResourceManager()->CreateTexture(loadedTexture,ImageFormat::RGBA8,vk::Filter::eNearest,{},true);
            }

            {
                auto img = newShared<ImageWidget>();
                img->SetTextureId(textureId);
                auto fitter = (newShared<WidgetFitter>() + img).second;
                fitter->SetPadding(Padding{20.0f});
                auto slot = (panel + fitter).first;
                slot->minAnchor = Vec2{0.0f,0.0f};
                slot->maxAnchor = Vec2{0.5f,1.0f};
                slot->alignment = Vec2{0.0f};
                fitter->SetClipMode(EClipMode::Bounds);
                fitter->SetMode(FitMode::Cover);
            }
            {
                auto img = newShared<ImageWidget>();
                img->SetTextureId(textureId);
                auto fitter = (newShared<WidgetFitter>() + img).second;
                fitter->SetPadding(Padding{20.0f});
                auto slot = (panel + fitter).first;
                slot->minAnchor = Vec2{0.5f,0.0f};
                slot->maxAnchor = Vec2{1.0f,1.0f};
                slot->alignment = Vec2{0.0f};
                fitter->SetClipMode(EClipMode::Bounds);
                fitter->SetMode(FitMode::Cover);
            }
        }
        panel->SetClipMode(EClipMode::Bounds);
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
