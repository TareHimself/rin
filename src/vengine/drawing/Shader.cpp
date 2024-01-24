#include "Shader.hpp"
#include "Drawer.hpp"
#include <spirv_cross/spirv_cpp.hpp>
#include <utility>

namespace vengine::drawing {

vk::ShaderModule Shader::Get() const {
  return _vkShader;
}

std::filesystem::path Shader::GetSourcePath() const {
  return _sourcePath;
}

Shader::operator vk::ShaderModule() const {
  return this->Get();
}

void Shader::Use() {
  ++_refs;
}

bool Shader::RemoveUsage() {
  return --_refs == 0;
}

void Shader::SetVulkanShader(const vk::ShaderModule shader) {
  _vkShader = shader;
}

void Shader::SetSourcePath(const std::filesystem::path &path) {
  _sourcePath = path;
}

void Shader::SetResources(const ShaderResources &resources) {
  _resources = resources;
}

vk::ShaderStageFlagBits Shader::GetStage() const {
  return _stage;
}

void Shader::Init(ShaderManager *outer) {
  Object<ShaderManager>::Init(outer);
  switch (auto stage = ShaderManager::GetLang(_sourcePath)) {
  case EShLangCompute:
      _stage = vk::ShaderStageFlagBits::eCompute;
    break;
  case EShLangVertex:
    _stage = vk::ShaderStageFlagBits::eVertex;
    break;
  case EShLangFragment:
    _stage = vk::ShaderStageFlagBits::eFragment;
    break;
    default:
      _stage = vk::ShaderStageFlagBits::eVertex;
  }
}

void Shader::Destroy() {
  if(RemoveUsage()) {
    GetOuter()->UnRegisterShader(this);
    Object<ShaderManager>::Destroy();
  }
}

ShaderResources Shader::GetResources() const {
  return _resources;
}


void Shader::HandleDestroy() {
  Object<ShaderManager>::HandleDestroy();
  GetOuter()->GetOuter()->GetDevice().destroyShaderModule(this->Get());
}


Shader * Shader::FromSource(ShaderManager *manager,
                            const std::filesystem::path &path) {
  if(const auto existingShader = manager->GetLoadedShader(path)) {
    existingShader->Use();
    return existingShader;
  }
  
  const auto spvData = manager->LoadOrCompileSpv(path);

  const spirv_cross::CompilerGLSL glsl(spvData);
  
  spirv_cross::ShaderResources resources = glsl.get_shader_resources();
  ShaderResources newResources;

  for ( auto &resource : resources.sampled_images)
  {
    unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
    unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);
    
    newResources.images.insert({resource.name,{set,binding}});
  }

  for ( auto &resource : resources.push_constant_buffers)
  {
    newResources.pushConstants.insert({resource.name,{0,vk::ShaderStageFlags{}}});
  }

  for ( auto &resource : resources.uniform_buffers)
  {
    unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
    unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);
    
    newResources.uniformBuffers.insert({resource.name,{set,binding}});
  }
  
  const auto device = manager->GetOuter()->GetDevice();
  const auto shaderCreateInfo = vk::ShaderModuleCreateInfo(
      vk::ShaderModuleCreateFlags(),
      spvData.size() * sizeof(uint32_t), spvData.data());

  const auto shaderObj = newObject<Shader>();
  shaderObj->SetSourcePath(path);
  shaderObj->SetVulkanShader(device.createShaderModule(shaderCreateInfo));
  shaderObj->SetResources(newResources);
  shaderObj->Use();
  return manager->RegisterShader(shaderObj);
}
}
