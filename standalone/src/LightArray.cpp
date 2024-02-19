#include "LightArray.hpp"

#include "vengine/scene/components/PointLightComponent.hpp"

void LightArray::Init(scene::Scene *outer) {
  SceneObject::Init(outer);

  constexpr auto distance = 5000.0f;
  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({0,distance,0});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }

  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({0,distance * -1.0f,0});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }
  
  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({distance,0,0});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }
  
  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({distance * -1.0f,0,0});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }
  
  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({ 0,0,distance});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }
  
  if(auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().Reserve()) {
    pointLight->SetWorldLocation({ 0,0,distance * -1.0f});
    pointLight->AttachTo(GetRootComponent());
    lights.push(pointLight);
  }

  for(auto light : lights) {
    if(auto ref = light.Reserve()) {
      constexpr auto intensity = 5.0f;
      ref->SetIntensity(intensity);
      // if(auto lightComp = ref->GetRootComponent().Cast<scene::PointLightComponent>().Reserve()) {
      //   lightComp->SetIntensity(intensity);
      // }
    }
  }
}

void LightArray::Tick(float deltaTime) {
  SceneObject::Tick(deltaTime);

  SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime));
}
