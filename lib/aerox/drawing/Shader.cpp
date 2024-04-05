#include "aerox/Engine.hpp"

#include <aerox/drawing/Shader.hpp>
#include <aerox/drawing/DrawingSubsystem.hpp>
#include <spirv_cross/spirv_glsl.hpp>
#include <utility>

namespace aerox::drawing {

vk::ShaderModule Shader::Get() const {
  return _vkShader;
}

fs::path Shader::GetSourcePath() const {
  return _sourcePath;
}

Shader::operator vk::ShaderModule() const {
  return this->Get();
}

void Shader::SetVulkanShader(const vk::ShaderModule shader) {
  _vkShader = shader;
}

void Shader::SetSourcePath(const fs::path &path) {
  _sourcePath = path;
}

void Shader::SetResources(const ShaderResources &resources) {
  _resources = resources;
}

vk::ShaderStageFlagBits Shader::GetStage() const {
  return _stage;
}

void Shader::OnInit(ShaderManager * manager) {
  TOwnedBy::OnInit(manager);
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


ShaderResources Shader::GetResources() const {
  return _resources;
}


void Shader::OnDestroy() {
  TOwnedBy::OnDestroy();
  GetOwner()->UnRegisterShader(this);
  GetOwner()->GetOwner()->GetVirtualDevice().destroyShaderModule(this->Get());
}


std::shared_ptr<Shader> Shader::FromSource(
    const fs::path &path) {
  auto manager = Engine::Get()->GetDrawingSubsystem().lock()->GetShaderManager().lock();
  
  if(auto existingShader = manager->GetLoadedShader(path)) {
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
    auto numArray = glsl.get_type(resource.type_id).array;
    auto numRequired = std::max(numArray.empty() ? 0 : numArray[0],static_cast<uint32_t>(1));
    
    newResources.images.insert({resource.name,{set,binding,numRequired}});
  }

  for ( auto &resource : resources.push_constant_buffers)
  {
    uint32_t size = glsl.get_declared_struct_size(glsl.get_type(resource.base_type_id));
    uint32_t offset = 0; // Needs work glsl.get_decoration(resource.id,spv::DecorationOffset);
    
    newResources.pushConstants.insert({resource.name,{offset,size,vk::ShaderStageFlags{}}});
  }

  for ( auto &resource : resources.uniform_buffers)
  {
    unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
    unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);
    
    auto numArray = glsl.get_type(resource.type_id).array;
    auto numRequired = std::max(numArray.empty() ? 0 : numArray[0],static_cast<uint32_t>(1));
    
    newResources.uniformBuffers.insert({resource.name,{set,binding,numRequired}});
  }
  
  const auto device = Engine::Get()->GetDrawingSubsystem().lock()->GetVirtualDevice();
  const auto shaderCreateInfo = vk::ShaderModuleCreateInfo(
      vk::ShaderModuleCreateFlags(),
      spvData.size() * sizeof(uint32_t), spvData.data());

  const auto shaderObj = newObject<Shader>();
  shaderObj->SetSourcePath(path);
  shaderObj->SetVulkanShader(device.createShaderModule(shaderCreateInfo));
  shaderObj->SetResources(newResources);
  return manager->RegisterShader(shaderObj);
}
}
