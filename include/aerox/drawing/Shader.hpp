#pragma once
#include "ShaderManager.hpp"
#include "types.hpp"
#include "aerox/Object.hpp"
#include <vulkan/vulkan.hpp>
#include <aerox/fs.hpp>
#include "gen/drawing/Shader.gen.hpp"
namespace aerox::drawing {
class ShaderManager;

META_TYPE()
class Shader : public TOwnedBy<ShaderManager> {
  fs::path _sourcePath;
  vk::ShaderModule _vkShader;
  ShaderResources _resources;
  vk::ShaderStageFlagBits _stage{};
  std::atomic<std::uint64_t> _refs = 0;
public:

  META_BODY()
  
  vk::ShaderModule Get() const;
  fs::path GetSourcePath() const;
  
  operator vk::ShaderModule() const;
  void SetVulkanShader(vk::ShaderModule shader);
  void SetSourcePath(const fs::path &path);
  void SetResources(const ShaderResources& resources);
  vk::ShaderStageFlagBits GetStage() const;
  void OnInit(ShaderManager * manager) override;

  ShaderResources GetResources() const;
  void OnDestroy() override;
  
  static std::shared_ptr<Shader> FromSource(
      const fs::path &path);
};
}
