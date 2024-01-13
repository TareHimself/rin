#pragma once
#include "ShaderManager.hpp"
#include "vengine/Object.hpp"
#include <vulkan/vulkan.hpp>
#include <filesystem>

namespace vengine {
namespace drawing {
class ShaderManager;

class Shader : public Object<ShaderManager> {
  std::filesystem::path _sourcePath;
  vk::ShaderModule _vkShader;
public:
  vk::ShaderModule get() const;
  std::filesystem::path getSourcePath() const;
  
  operator vk::ShaderModule() const;
  
  void setVulkanShader(vk::ShaderModule shader);
  void setSourcePath(const std::filesystem::path &path);
  
  void handleCleanup() override;
  
  static Shader * fromSource(ShaderManager * manager,const std::filesystem::path &path);
};
}
}
