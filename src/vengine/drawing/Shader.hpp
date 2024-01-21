#pragma once
#include "ShaderManager.hpp"
#include "vengine/Object.hpp"
#include <vulkan/vulkan.hpp>
#include <filesystem>

namespace vengine::drawing {
class ShaderManager;

class Shader : public Object<ShaderManager> {
  std::filesystem::path _sourcePath;
  vk::ShaderModule _vkShader;
  ShaderResources _resources;
public:
  
  vk::ShaderModule Get() const;
  std::filesystem::path GetSourcePath() const;
  
  operator vk::ShaderModule() const;
  
  void SetVulkanShader(vk::ShaderModule shader);
  void SetSourcePath(const std::filesystem::path &path);
  void SetResources(const ShaderResources& resources);

  ShaderResources GetResources() const;
  void HandleDestroy() override;
  
  static Shader * FromSource(ShaderManager * manager,const std::filesystem::path &path);
};
}
