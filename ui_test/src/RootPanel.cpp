#include "RootPanel.hpp"

#include "aerox/Engine.hpp"
#include "aerox/assets/AssetSubsystem.hpp"
#include "aerox/io/IoSubsystem.hpp"
#include "aerox/widgets/Button.hpp"
#include "aerox/widgets/Image.hpp"
#include "aerox/widgets/Overlay.hpp"
#include "aerox/widgets/Text.hpp"

void RootPanel::OnInit(WidgetSubsystem *owner) {
  Panel::OnInit(owner);
  const auto assets = GetOwner()->GetOwner()->GetAssetSubsystem().lock();
  _font = assets->ImportFont(R"(D:\Github\vengine\local\Noto_Sans\NotoSans-Italic-VariableFont_wdth,wght.ttf)");
  const auto sizer = GetOwner()->CreateWidget<Sizer>();
  const auto button = GetOwner()->CreateWidget<Button>();
  auto buttonSlot = sizer->AddChild(button);
  const auto sizerSlot = AddChild(sizer).lock();
  sizerSlot->SetAlignment({0.5,0.5});
  sizerSlot->SetMaxAnchor({0.5,0.5});
  sizerSlot->SetMinAnchor({0.5,0.5});
  sizerSlot->SetSizeToContent(true);
  
  const auto img = GetOwner()->CreateWidget<Image>();
  const auto overl = GetOwner()->CreateWidget<Overlay>();
  overl->AddChild(img);
  const auto text = GetOwner()->CreateWidget<Text>();
  text->SetContent("PUSH ME");
  text->SetFont(_font);
  text->SetFontSize(100);
  text->SetColor({1.0,0.0,1.0,1.0});
  overl->AddChild(text);
  button->AddChild(overl);
  // std::vector<fs::path> textures;
  // vengine::io::IoSubsystem::SelectFiles(textures,false,"Select Texture","*.jpg;*.png;*.bmp");
  // if(!textures.empty()) {
  //   img->SetTexture(GetOwner()->GetOwner()->GetAssetSubsystem().lock()->ImportTexture(textures.front()));
  // } else {
  //   sizer->SetWidth(500);
  //   sizer->SetHeight(150);
  // }

  sizer->SetWidth(500);
  sizer->SetHeight(150);
  //button->AddChild(img);
  std::vector<Color> colors = {{1.0f,1.0f,1.0f,1.0f},{1.0f,0.0f,0.0f,1.0f},{0.0f,1.0f,0.0f,1.0f},{0.0f,0.0f,1.0f,1.0f}};
  button->onReleased->BindFunction([colors,this,img](const std::shared_ptr<Button> & b, const std::shared_ptr<vengine::window::MouseButtonEvent> & e) {
    currentColor = (currentColor + 1) % colors.size();
    img->SetTint(colors[currentColor]);
  });
}
