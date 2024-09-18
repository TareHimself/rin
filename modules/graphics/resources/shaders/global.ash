

layout(set = 0,binding = 0) uniform sampler2D GLOBAL_TEXTURES[];

vec4 sampleTexture(int textureId,float2 uv) -> texture( GLOBAL_TEXTURES[textureId], uv );
