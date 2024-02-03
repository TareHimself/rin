// layout(location = 0) in vec3 iLocation;
// layout(location = 1) in vec3 iColor;
// layout(location = 2) in vec3 iNormal;
// layout(location = 3) in vec2 iUV;
struct Light {
  vec4 location;
  vec4 direction;
  vec4 color;
};


layout(set = 0, binding = 0) uniform  SceneGlobalBuffer{   

	mat4 viewMatrix;
	mat4 projectionMatrix;
	vec4 ambientColor;
	vec4 lightDirection;
	vec4 cameraLocation;
  vec4 numLights;
  Light lights[1024];
} scene;
