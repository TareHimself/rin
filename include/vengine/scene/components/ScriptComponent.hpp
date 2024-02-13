#pragma once
#include "SceneComponent.hpp"
#include "vengine/scripting/Script.hpp"
#include <filesystem>
#include "generated/scene/components/ScriptComponent.reflect.hpp"
namespace vengine::scene {
RCLASS()
class ScriptComponent : public SceneComponent {
  std::filesystem::path _scriptPath;
  Managed<scripting::Script> _script;

public:
  ScriptComponent();
  void Init(SceneObject *outer) override;
  ScriptComponent(const std::filesystem::path &path);

  RFUNCTION()
  static Managed<ScriptComponent> Construct() {
    return newManagedObject<ScriptComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(ScriptComponent)
};

REFLECT_IMPLEMENT(ScriptComponent)
}
