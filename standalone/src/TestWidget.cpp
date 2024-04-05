#include "TestWidget.hpp"

#include "Test.hpp"
#include "aerox/Engine.hpp"
#include "aerox/assets/AssetSubsystem.hpp"
#include "aerox/async/AsyncSubsystem.hpp"
#include "aerox/async/Task.hpp"
#include "aerox/drawing/Texture.hpp"
#include "aerox/io/IoSubsystem.hpp"
#include "aerox/widgets/Column.hpp"
#include "aerox/widgets/Image.hpp"
#include "aerox/widgets/Sizer.hpp"
#include "aerox/widgets/Text.hpp"
#include "aerox/widgets/Viewport.hpp"
using namespace aerox::widgets;

void TestWidget::OnInit(aerox::widgets::WidgetSubsystem * ref) {
  Widget::OnInit(ref);

  const auto viewport = GetOwner()->CreateWidget<Viewport>();
  if (auto viewportSlot = AddChild(viewport).lock()) {
    viewportSlot->SetMinAnchor({0, 0});
    viewportSlot->SetMaxAnchor({1, 1});
  }
  auto columnSizer = GetOwner()->CreateWidget<Sizer>();

  //columnSizer->SetWidth(350.0f);

  _row = GetOwner()->CreateWidget<Row>();

  columnSizer->AddChild(_row);

  auto rowSlot = this->AddChild(columnSizer).lock();
  rowSlot->SetMinAnchor({0.0, 0.0});
  rowSlot->SetMaxAnchor({0.0, 0.0});
  rowSlot->SetAlignment({0.0f, 0.0f});
  ///**rowSlot->SetSizeToContent(true);
  rowSlot->SetSize({100.0_ru,100.0_ru});
  _font = Engine::Get()->GetAssetSubsystem().lock()->ImportFont(R"(D:\Github\vengine\NotoSans-VariableFont_wdth,wght.ttf)");
  _fpsText = GetOwner()->CreateWidget<Text>();
  _fpsText->SetFont(_font);
  _fpsText->SetFontSize(70.0f);
  auto textSlot = this->AddChild(_fpsText).lock();
  textSlot->SetAlignment({1.0f,0.0f});
  textSlot->SetMaxAnchor({1.0f,0.0f});
  textSlot->SetMinAnchor({1.0f,0.0f});
  textSlot->SetSizeToContent(true);
}

void TestWidget::BindInput(
    const std::weak_ptr<aerox::window::Window> &window) {
  AddCleanup(window.lock()->onKeyDown->BindFunction(
      [this](const std::shared_ptr<aerox::window::KeyEvent> &e) {
        switch (e->key) {
        case aerox::window::Key_F: {
          auto widgetManager =
              aerox::Engine::Get()->
              GetWidgetSubsystem().lock();

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
              AddChild(shaderWidget);

          // sizer->SetWidth(imageWidth);
          // sizer->SetHeight(imageHeight);

          _row->AddChild(sizer);
        }
        break;
        case aerox::window::Key_G: {

          const auto task = async::newTask<std::shared_ptr<drawing::Texture>>([&] {
            const auto assetImporter = Engine::Get()->
                                       GetAssetSubsystem().lock();

            std::vector<fs::path> files;
            
            io::IoSubsystem::SelectFiles(files,false,"Select Texture","*.png;*.jpeg;*.jpg;*.bmp");

            if(files.empty()) {
              throw std::runtime_error("User did not select a file");
            }
            
            return assetImporter
              ->ImportTexture(files.front());
          });

          task->onCompleted->BindFunction([this](const std::shared_ptr<drawing::Texture>& background) {
            const auto widgetManager = Engine::Get()->
                                       GetWidgetSubsystem().lock();
          

          const auto sizer = widgetManager->
              CreateWidget
              <widgets::Sizer>();
          const auto image = widgetManager->
              CreateWidget
              <widgets::Image>();
          sizer->
              AddChild(image);
            
          image->SetTexture(background);

          const auto textureDims = background->GetSize();
          constexpr int imageHeight = 500.0f;
          const auto imageWidth =
              static_cast<float>(textureDims.
                width) /
              static_cast<float>(textureDims.
                height)
              * imageHeight;

            sizer->SetWidth(
              static_cast<float>(imageWidth));
            sizer->SetHeight(
                static_cast<float>(
                  imageHeight));

            _row->AddChild(sizer);
          });

          task->onException->BindFunction([](std::exception &e) {
             log::engine->Error("Error while loading texture {}",e.what());
          });
          task->Enqueue();
        }
        break;
        case aerox::window::Key_H: {
          auto assetImporter = Engine::Get()->
                               GetAssetSubsystem().lock();
          auto widgetManager =
              aerox::Engine::Get()->
              GetWidgetSubsystem().lock();
          auto text = widgetManager->CreateWidget<Text>();
           text->SetContent("Hello World.");
           text->SetFont(_font);
          text->SetFontSize(100);
          _row->AddChild(text);
        }
        break;
        }

        return true;
      }));
}

void TestWidget::Tick(float deltaTime) {
  Panel::Tick(deltaTime);
  _fps.push_back(std::min(1.0f / deltaTime,10000.0f));
  if(_fps.size() > 10) {
    _fps.pop_front();
  }

  float avg = 0.0f;
  for(const auto &f : _fps) {
    avg += f;
  }
  
  avg /= _fps.size();
  
  _fpsText->SetContent(std::to_string(static_cast<int>(avg)) + " FPS");
}
