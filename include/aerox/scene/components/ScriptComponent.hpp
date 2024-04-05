#pragma once
#include "SceneComponent.hpp"
#include "aerox/scripting/Script.hpp"
#include <aerox/fs.hpp>
#include "gen/scene/components/ScriptComponent.gen.hpp"
namespace aerox::scene {
META_TYPE()
class ScriptComponent : public SceneComponent {
  fs::path _scriptPath;
  std::shared_ptr<scripting::Script> _script;

public:

  META_BODY()
  
  ScriptComponent();
  void OnInit(SceneObject * owner) override;
  ScriptComponent(const fs::path &path);

  META_FUNCTION()
  static std::shared_ptr<ScriptComponent> Construct() {
    return newObject<ScriptComponent>();
  }
};
}
