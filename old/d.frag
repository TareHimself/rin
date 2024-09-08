#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_scalar_block_layout : require
#define RECIPROCAL_PI 0.31830987
#define RECIPROCAL_PI 0.31830987
struct Light{
vec4 location;
vec4 direction;
vec4 color;
};
layout(set=0 , binding=0 , scalar) uniform SceneGlobalBuffer {
mat4 viewMatrix;
mat4 projectionMatrix;
vec4 ambientColor;
vec4 lightDirection;
vec4 cameraLocation;
int numLights;
Light lights[1024];
} scene;
vec3 fresnelSchlick(in float cosTheta , in vec3 F0){
return F0 + ( 1.0 - F0 ) * pow( 1.0 - cosTheta , 5.0 );
}
float D_GGX(in float NoH , in float roughness){
float alpha = roughness * roughness;
float alpha2 = alpha * alpha;
float NoH2 = NoH * NoH;
float b = ( NoH2 * ( alpha2 - 1.0 ) + 1.0 );
return alpha2 * RECIPROCAL_PI / ( b * b );
}
float G1_GGX_Schlick(in float NoV , in float roughness){
float alpha = roughness * roughness;
float k = alpha / 2.0;
return max( NoV , 0.001 ) / ( NoV * ( 1.0 - k ) + k );
}
float G_Smith(in float NoV , in float NoL , in float roughness){
return G1_GGX_Schlick( NoL , roughness ) * G1_GGX_Schlick( NoV , roughness );
}
float fresnelSchlick90(in float cosTheta , in float F0 , in float F90){
return F0 + ( F90 - F0 ) * pow( 1.0 - cosTheta , 5.0 );
}
float disneyDiffuseFactor(in float NoV , in float NoL , in float VoH , in float roughness){
float alpha = roughness * roughness;
float F90 = 0.5 + 2.0 * alpha * VoH * VoH;
float F_in = fresnelSchlick90( NoL , 1.0 , F90 );
float F_out = fresnelSchlick90( NoV , 1.0 , F90 );
return F_in * F_out;
}
vec3 computeLightRadiance(in vec3 sceneLocation , in vec3 color , in Light light , in float NoV , in vec3 normal , in vec3 viewDir , in float roughness , in float metallic){
vec3 lightDirection = light.location.xyz - sceneLocation;
vec3 lightDir = normalize( -lightDirection );
float irradiPerp = light.color.w;
float irradiance = max( dot( lightDir , normal ) , 0.0 ) * irradiPerp;
if(irradiance > 0.0)
{
vec3 H = normalize( viewDir + lightDir );
float NoL = clamp( dot( normal , lightDir ) , 0.0 , 1.0 );
float NoH = clamp( dot( normal , H ) , 0.0 , 1.0 );
float VoH = clamp( dot( viewDir , H ) , 0.0 , 1.0 );
vec3 f0 = vec3( 0.16 * ( roughness * roughness ) );
f0 = mix( f0 , color , metallic );
vec3 F = fresnelSchlick( VoH , f0 );
float D = D_GGX( NoH , roughness );
float G = G_Smith( NoV , NoL , roughness );
vec3 spec = ( F * D * G ) / ( 4.0 * max( NoV , 0.001 ) * max( NoL , 0.001 ) );
vec3 rhoD = color;
rhoD *= vec3( 1.0 ) - F;
rhoD *= disneyDiffuseFactor( NoV , NoL , VoH , roughness );
rhoD *= ( 1.0 - metallic );
vec3 diff = rhoD * RECIPROCAL_PI;
return ( diff + spec ) * irradiance * light.color.xyz;
}
return vec3( 0.0 );
}
vec4 computePixelColor(in vec3 sceneLocation , in vec3 color , in vec3 normal , in float roughness , in float metallic , in vec3 emissive){
vec3 viewDir = normalize( scene.cameraLocation.xyz - sceneLocation );
float NoV = clamp( dot( normal , viewDir ) , 0.0 , 1.0 );
vec3 radiance = emissive;
for(int i = 0;i < scene.numLights;++i)
{
radiance += computeLightRadiance( sceneLocation , color , scene.lights[i] , NoV , normal , viewDir , roughness , metallic );
}
return vec4( radiance , 1.0 );
}
layout(set=1 , binding=0) uniform sampler2D TColor;
layout(set=1 , binding=1) uniform sampler2D TLocation;
layout(set=1 , binding=2) uniform sampler2D TNormal;
layout(set=1 , binding=3) uniform sampler2D TRoughMetallicSpecular;
layout(set=1 , binding=5) uniform sampler2D TEmissive;
layout(location=0) in vec2 iUV;
layout(location=0) out vec4 oColor;
void main(){
vec3 sceneLocation = texture( TLocation , iUV ).xyz;
vec3 color = texture( TColor , iUV ).xyz;
vec3 normal = texture( TNormal , iUV ).xyz;
vec3 roughMetallicSpecular = texture( TRoughMetallicSpecular , iUV ).xyz;
vec3 emissive = texture( TEmissive , iUV ).xyz;
ro oColor = computePixelColor( sceneLocation , color , normal , roughMetallicSpecular.r , roughMetallicSpecular , emissive );
}
