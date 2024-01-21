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

void Shader::SetVulkanShader(const vk::ShaderModule shader) {
  _vkShader = shader;
}

void Shader::SetSourcePath(const std::filesystem::path &path) {
  _sourcePath = path;
}

void Shader::SetResources(const ShaderResources &resources) {
  _resources = resources;
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
    
    if(set == 0) continue;
    
    newResources.images.insert({resource.name,{set,binding}});
  }

  for ( auto &resource : resources.push_constant_buffers)
  {
    newResources.pushConstants.insert({resource.name,{-1,-1}});
  }

  for ( auto &resource : resources.uniform_buffers)
  {
    unsigned set = glsl.get_decoration(resource.id, spv::DecorationDescriptorSet);
    unsigned binding = glsl.get_decoration(resource.id, spv::DecorationBinding);

    if(set == 0) continue;
    
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
  
  return manager->RegisterShader(shaderObj);
}
}
