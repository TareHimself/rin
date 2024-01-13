#include "Shader.hpp"
#include "Drawer.hpp"

namespace vengine {
namespace drawing {

vk::ShaderModule Shader::get() const {
  return _vkShader;
}

std::filesystem::path Shader::getSourcePath() const {
  return _sourcePath;
}

Shader::operator vk::ShaderModule() const {
  return this->get();
}

void Shader::setVulkanShader(const vk::ShaderModule shader) {
  _vkShader = shader;
}

void Shader::setSourcePath(const std::filesystem::path &path) {
  _sourcePath = path;
}


void Shader::handleCleanup() {
  Object<ShaderManager>::handleCleanup();
  getOuter()->getOuter()->getDevice().destroyShaderModule(this->get());
}


Shader * Shader::fromSource(ShaderManager *manager,
                          const std::filesystem::path &path) {
  if(const auto existingShader = manager->getLoadedShader(path)) {
    return existingShader;
  }
  
  const auto spvData = manager->loadOrCompileSpv(path);
  const auto device = manager->getOuter()->getDevice();
  const auto shaderCreateInfo = vk::ShaderModuleCreateInfo(
      vk::ShaderModuleCreateFlags(),
      spvData.size() * sizeof(uint32_t), spvData.data());

  const auto shaderObj = newObject<Shader>();
  shaderObj->setSourcePath(path);
  shaderObj->setVulkanShader(device.createShaderModule(shaderCreateInfo));
  
  return manager->registerShader(shaderObj);
}
}
}
