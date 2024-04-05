#pragma once
#include "aerox/scene/Scene.hpp"
#include "aerox/widgets/Widget.hpp"
using namespace vengine::scene;
using namespace vengine::widgets;

class MainScene : public Scene{
public:
  void OnInit(vengine::Engine *owner) override;
};
