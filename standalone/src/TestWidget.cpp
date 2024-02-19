#include "TestWidget.hpp"

#include "Test.hpp"
#include "vengine/Engine.hpp"
#include "vengine/assets/AssetSubsystem.hpp"
#include "vengine/containers/TAsyncTask.hpp"
#include "vengine/drawing/Texture2D.hpp"
#include "vengine/widget/Column.hpp"
#include "vengine/widget/Image.hpp"
#include "vengine/widget/Sizer.hpp"
#include "vengine/widget/Text.hpp"
using namespace vengine::widget;


void TestWidget::Init(WidgetSubsystem *outer) {
  Panel::Init(outer);
  auto columnSizer = outer->CreateWidget<Sizer>();

  //columnSizer->SetWidth(350.0f);

  _row = outer->CreateWidget<Row>();

  columnSizer->Add(_row);

  auto rowSlot = this->Add(columnSizer).Reserve();
  rowSlot->SetMinAnchor({0.0, 0.5});
  rowSlot->SetMaxAnchor({1.0, 0.5});
  rowSlot->SetAlignment({0.0f, 0.0f});
  rowSlot->SetSizeToContent(true);

}

void TestWidget::BindInput(
    const vengine::Ref<vengine::window::Window> &window) {
  AddCleanup(window.Reserve()->onKeyDown, window.Reserve()->onKeyDown.Bind(
                 [this](std::shared_ptr<vengine::window::KeyEvent> e) {
                   switch (e->key) {
                   case vengine::window::Key_F: {
                     vengine::AsyncTaskManager::Get()->CreateTask<
                       void *>(
                         [this] {
                           auto widgetManager =
                               vengine::Engine::Get()->
                               GetWidgetSubsystem().
                               Reserve();

                           const auto sizer = widgetManager
                               ->
                               CreateWidget
                               <Sizer>();
                           const auto shaderWidget =
                               widgetManager
                               ->
                               CreateWidget
                               <PrettyShader>();
                           sizer->
                               Add(shaderWidget);

                           // sizer->SetWidth(imageWidth);
                           // sizer->SetHeight(imageHeight);

                           _row->Add(sizer);
                           return nullptr;
                         })->Run();
                   }
                   break;
                   case vengine::window::Key_G: {
                     AsyncTaskManager::Get()->CreateTask<
                       void *>([this] {
                       auto assetImporter = Engine::Get()->
                                            GetAssetSubsystem().Reserve();
                       auto widgetManager = Engine::Get()->
                                            GetWidgetSubsystem().Reserve();
                       const auto background =
                           assetImporter
                           ->
                           ImportTexture(
                               R"(C:\Users\Taree\Pictures\Wallpaperz\silhouette of steel ridge wallpaper, blue and pink sky painting 7b507f40-43f9-484f-9bd9-16b7443428cd.jpg)");

                       const auto sizer = widgetManager->
                           CreateWidget
                           <widget::Sizer>();
                       const auto image = widgetManager->
                           CreateWidget
                           <widget::Image>();
                       sizer->
                           Add(image);

                       auto actualImage = image;

                       actualImage->SetTexture(background);

                       const auto textureDims = background->GetSize();
                       constexpr int imageHeight = 250.0f;
                       const auto imageWidth =
                           static_cast<float>(textureDims.
                             width) /
                           static_cast<float>(textureDims.
                             height)
                           * imageHeight;

                       // sizerSlot->SetRect(
                       //     {0, 0, {imageWidth, imageHeight}});

                       sizer->SetWidth(
                           static_cast<float>(imageWidth));
                       sizer->SetHeight(
                           static_cast<float>(
                             imageHeight));

                       _row->Add(sizer);
                       return nullptr;
                     })->Run();
                   }
                   break;
                   case vengine::window::Key_H: {
                     auto assetImporter = Engine::Get()->
                                            GetAssetSubsystem().Reserve();
                     auto widgetManager =
                             vengine::Engine::Get()->
                             GetWidgetSubsystem().
                             Reserve();
                     auto text = widgetManager->CreateWidget<Text>();
                     text->SetContent("This is a test!");
                     text->SetFont(assetImporter->ImportFont(R"(D:\Github\vengine\fonts\noto)"));
                     _row->Add(text);
                 }
                 break;
                   }
                   

                   return true;
                 }));
}
