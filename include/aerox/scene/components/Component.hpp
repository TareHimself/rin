#pragma once
#include "aerox/Object.hpp"
#include "aerox/TObjectWithInit.hpp"
#include "aerox/TOwnedBy.hpp"
#include "aerox/containers/Serializable.hpp"
#include "gen/scene/components/Component.gen.hpp"
namespace aerox::scene {
class SceneObject;
}

namespace aerox::scene {

META_TYPE()
class Component : public TOwnedBy<SceneObject>, public Serializable {
public:

  META_BODY()
  
  void OnInit(SceneObject * owner) override;
  
  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;
};

}
