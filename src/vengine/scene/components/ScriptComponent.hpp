#pragma once
#include "SceneComponent.hpp"
#include "vengine/scripting/Script.hpp"
#include <filesystem>

namespace vengine::scene {
class ScriptComponent : public SceneComponent {
  std::filesystem::path _scriptPath;
  scripting::Script * _script = nullptr;
public:
  void Init(SceneObject *outer) override;
  ScriptComponent(const std::filesystem::path &path);
};
}
