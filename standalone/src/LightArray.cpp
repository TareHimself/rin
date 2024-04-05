#include "LightArray.hpp"

#include "aerox/scene/components/PointLightComponent.hpp"


void LightArray::Tick(float deltaTime) {
  SceneObject::Tick(deltaTime);

  SetWorldRotation(GetWorldRotation().ApplyYaw(70.f * deltaTime));
}

void LightArray::OnInit(scene::Scene * scene) {
    SceneObject::OnInit(scene);
    constexpr auto distance = 5000.0f;

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({0,distance,0});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({0,distance * -1.0f,0});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({distance,0,0});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({distance * -1.0f,0,0});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({ 0,0,distance});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    if(const auto pointLight = GetScene()->CreateSceneObject<scene::PointLight>().lock()) {
        pointLight->SetWorldLocation({ 0,0,distance * -1.0f});
        pointLight->AttachTo(GetRootComponent());
        lights.push(pointLight);
    }

    for(const auto &light : lights) {
        if(const auto ref = light.lock()) {
            constexpr auto intensity = 5.0f;
            ref->SetIntensity(intensity);
            // if(auto lightComp = ref->GetRootComponent().Cast<scene::PointLightComponent>().Reserve()) {
            //   lightComp->SetIntensity(intensity);
            // }
        }
    }

}
