#ifndef VENGINE_DRAWING_SCENE_TYPES
#define VENGINE_DRAWING_SCENE_TYPES
#include "vengine/drawing/types.hpp"

namespace vengine::drawing {
class SceneDrawer;
struct GpuLight {
  glm::vec4 location;
  glm::vec4 direction;
  glm::vec4 color;
};
struct SceneGlobalBuffer {
  glm::mat4 viewMatrix;
  glm::mat4 projectionMatrix;
  glm::vec4 ambientColor{1.0f,1.0f,1.0f,0.02f};
  glm::vec4 lightDirection{0.0f,-1.0f,0,0.0f};
  glm::vec4 cameraLocation{0.0f};
  glm::vec4 numLights{0.0f};
  GpuLight lights[1024];
};
struct SceneFrameData;

typedef std::function<void(SceneFrameData *)> drawFn;

struct SceneFrameData : SimpleFrameData {
  SceneDrawer * _sceneDrawer = nullptr;
public:
  SceneFrameData(RawFrameData * frame,SceneDrawer * drawer);

  SceneDrawer * GetSceneDrawer() const;

  Array<drawFn> lit;
  Array<drawFn> translucent;

  
  void AddLit(const drawFn& litFn);
  void AddTranslucent(const drawFn& translucentFn);
};

struct DrawList {
  
};

}
#endif