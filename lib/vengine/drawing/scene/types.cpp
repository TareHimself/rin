#include <vengine/drawing/scene/types.hpp>

namespace vengine::drawing {

SceneFrameData::SceneFrameData(RawFrameData *frame, SceneDrawer *drawer) : SimpleFrameData(frame){
  _sceneDrawer = drawer;
}

SceneDrawer * SceneFrameData::GetSceneDrawer() const {
  return _sceneDrawer;
}
}
