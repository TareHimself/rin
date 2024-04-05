#include <aerox/drawing/scene/types.hpp>

namespace aerox::drawing {

SceneFrameData::SceneFrameData(RawFrameData *frame, SceneDrawer *drawer) : SimpleFrameData(frame){
  _sceneDrawer = drawer;
}

SceneDrawer * SceneFrameData::GetSceneDrawer() const {
  return _sceneDrawer;
}

void SceneFrameData::AddLit(const drawFn &litFn) {
  lit.push(litFn);
}

void SceneFrameData::AddTranslucent(const drawFn &translucentFn) {
  translucent.push(translucentFn);
}
}
