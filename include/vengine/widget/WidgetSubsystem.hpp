#pragma once
#include "vengine/Object.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/window/Window.hpp"
#include "generated/widget/WidgetSubsystem.reflect.hpp"

namespace vengine {
namespace widget {
class WidgetRoot;
class Panel;
}
}

namespace vengine {
namespace widget {
class Font;
}
}

namespace vengine::widget {
class Widget;

RCLASS()
class WidgetSubsystem : public EngineSubsystem, public drawing::Drawable {
  std::unordered_map<uint64_t,Managed<WidgetRoot>> _roots;
  Array<Ref<WidgetRoot>> _rootsArr;
public:
  void Init(Engine * outer) override;

  void BeforeDestroy() override;

  String GetName() const override;

  void Draw(drawing::RawFrameData *frameData) override;
  
  template <typename T,typename... Args>
  Managed<T> CreateWidget(Args &&... args);

  void InitWidget(const Managed<Widget> &widget);

  Ref<WidgetRoot> GetRoot(const Ref<window::Window>& window);

  virtual void CreateRoot(const Ref<window::Window>& window);
  virtual void DestroyRoot(const Ref<window::Window>& window);
};

template <typename T, typename ... Args> Managed<T> WidgetSubsystem::CreateWidget(
    Args &&... args) {
  auto rawObj = newManagedObject<T>(std::forward<Args>(args)...);
  const auto obj = rawObj.template Cast<Widget>();
  InitWidget(obj);
  return rawObj;
}

REFLECT_IMPLEMENT(WidgetSubsystem)
}
