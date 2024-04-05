#pragma once
#include "aerox/Object.hpp"
#include "aerox/EngineSubsystem.hpp"
#include "aerox/drawing/Drawer.hpp"
#include "aerox/drawing/MaterialInstance.hpp"
#include "aerox/window/Window.hpp"
#include "gen/widgets/WidgetSubsystem.gen.hpp"



namespace aerox {
namespace widgets {
class WidgetRoot;
class Panel;
}
}

namespace aerox::widgets {
class Widget;

META_TYPE()
class WidgetSubsystem : public EngineSubsystem, public drawing::Drawer {
  std::unordered_map<uint64_t,std::shared_ptr<WidgetRoot>> _roots;
  Array<uint64_t> _rootsArr;
public:

  META_BODY()
  
  void OnInit(Engine * outer) override;

  void OnDestroy() override;

  String GetName() const override;

  void Draw(drawing::RawFrameData *frameData) override;
  
  template <typename T,typename... Args>
  TSharedConstruct<T,Args...> CreateWidget(Args &&... args);

  void InitWidget(const std::shared_ptr<Widget> &widget) const;

  std::weak_ptr<WidgetRoot> GetRoot(const std::weak_ptr<window::Window>& window);

  virtual void CreateRoot(const std::weak_ptr<window::Window>& window);
  virtual void DestroyRoot(const std::weak_ptr<window::Window>& window);

  std::shared_ptr<drawing::MaterialInstance> CreateMaterialInstance(const Array<std::shared_ptr<drawing::Shader>> &shaders);

  virtual void Tick(float deltaTime);
};

template <typename T, typename ... Args> TSharedConstruct<T,Args...> WidgetSubsystem::CreateWidget(
    Args &&... args) {
  auto rawObj = newObject<T>(std::forward<Args>(args)...);
  const auto obj = utils::castStatic<Widget>(rawObj);
  
  InitWidget(obj);
  return rawObj;
}
}
