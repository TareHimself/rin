#pragma once
#include "ShaderManager.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"
#include <vulkan/vulkan.hpp>
#include <filesystem>
#include "generated/drawing/Shader.reflect.hpp"
namespace vengine::drawing {
class ShaderManager;

RCLASS()
class Shader : public Object<ShaderManager> {
  std::filesystem::path _sourcePath;
  vk::ShaderModule _vkShader;
  ShaderResources _resources;
  vk::ShaderStageFlagBits _stage{};
  std::atomic<std::uint64_t> _refs = 0;
public:
  
  vk::ShaderModule Get() const;
  std::filesystem::path GetSourcePath() const;
  
  operator vk::ShaderModule() const;
  void SetVulkanShader(vk::ShaderModule shader);
  void SetSourcePath(const std::filesystem::path &path);
  void SetResources(const ShaderResources& resources);
  vk::ShaderStageFlagBits GetStage() const;
  void Init(ShaderManager * outer) override;

  ShaderResources GetResources() const;
  void BeforeDestroy() override;
  
  static Managed<Shader> FromSource(
      const std::filesystem::path &path);
};

REFLECT_IMPLEMENT(Shader)
}
