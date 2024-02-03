#pragma once
#include "SceneComponent.hpp"
#include "vengine/scripting/Script.hpp"
#include <filesystem>

namespace vengine::scene {
class ScriptComponent : public SceneComponent {
  std::filesystem::path _scriptPath;
  Ref<scripting::Script> _script;
public:
  ScriptComponent();
  void Init(SceneObject * outer) override;
  ScriptComponent(const std::filesystem::path &path);

  VENGINE_IMPLEMENT_COMPONENT_ID(ScriptComponent)
};


}
