#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Serializable.hpp"
namespace vengine::scene {
class SceneObject;
}

#ifndef VENGINE_IMPLEMENT_COMPONENT_ID
#define VENGINE_IMPLEMENT_COMPONENT_ID(Type) \
inline static String classId = #Type; \
String GetSerializeId() override { \
    return std::string("VENGINE_SCENE_OBJECT_COMPONENT_") + #Type; \
}
#endif

namespace vengine::scene {
class Component : public Object<SceneObject>, public Serializable {
public:
  SceneObject * GetOwner() const;
  
  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;
};

}
