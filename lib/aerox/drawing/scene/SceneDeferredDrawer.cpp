#include "aerox/drawing/scene/SceneDeferredDrawer.hpp"
#include "glm/gtx/transform2.hpp"
#include "aerox/drawing/MaterialBuilder.hpp"
#include "aerox/drawing/WindowDrawer.hpp"
#include "aerox/io/io.hpp"
#include "aerox/scene/components/CameraComponent.hpp"
#include "aerox/scene/components/LightComponent.hpp"
#include "aerox/scene/objects/SceneObject.hpp"

namespace aerox::drawing {

void GBuffer::Clear() {
  location.reset();
  color.reset();
  normals.reset();
  roughnessMetallic.reset();
  emissive.reset();
  specular.reset();
}

void SceneDeferredDrawer::OnInit(scene::Scene * owner) {
  SceneDrawer::OnInit(owner);
  

  auto drawer = GetDrawer().lock();
  _sceneGlobalBuffer = drawer->GetAllocator().lock()->CreateUniformCpuGpuBuffer<SceneGlobalBuffer>(false,"Scene Global Buffer");

  auto shaderManager = drawer->GetShaderManager().lock();
  _defaultCheckeredMaterial = CreateMaterialInstance({
    Shader::FromSource(io::getRawShaderPath("3d/mesh_deferred.frag")),
    Shader::FromSource(io::getRawShaderPath("3d/mesh.vert"))
  });
  
  
  const auto resources = _defaultCheckeredMaterial->GetResources();
  //
  for(const auto &key : resources.images | std::views::keys) {
    _defaultCheckeredMaterial->SetTexture(key,drawer->GetDefaultBlackTexture().lock());
  }
  //
  _defaultCheckeredMaterial->SetTexture("ColorT",drawer->GetDefaultErrorCheckerboardTexture().lock());
  //
  _defaultCheckeredMaterial->SetBuffer("SceneGlobalBuffer",_sceneGlobalBuffer);

  AddCleanup([this] {
    _defaultCheckeredMaterial.reset();
  });

  vk::SamplerCreateInfo samplerInfo{};
  samplerInfo.setMinFilter(vk::Filter::eNearest);
  samplerInfo.setMagFilter(vk::Filter::eNearest);

  _sampler = GetDrawer().lock()->GetVirtualDevice().createSampler(
      samplerInfo);

  AddCleanup([this] {
    GetDrawer().lock()->GetVirtualDevice().destroySampler(_sampler);
  });
  
  _shader = MaterialBuilder().AddShaders({
                                 Shader::FromSource(
                                     io::getRawShaderPath("3d/deferred.vert")),
                                 Shader::FromSource(
                                     io::getRawShaderPath("3d/deferred.frag"))
                             })
                             .SetType(EMaterialType::Lit)
                             .AddAttachmentFormats(
                                 {vk::Format::eR16G16B16A16Sfloat})
                             .Create();
  
  CreateBuffers();

  AddCleanup(GetWindowDrawer().lock()->onResizeScenes->BindFunction([this] {
               CreateBuffers();
             }));

  AddCleanup([this] {
    _defaultCheckeredMaterial.reset();
    _shader.reset();
    _sceneGlobalBuffer.reset();
    _gBuffer.Clear();
    _result.reset();
    _depth.reset();
  });
}

void SceneDeferredDrawer::CreateBuffers() {
  _gBuffer = {
      CreateBufferImage(),
      CreateBufferImage(),
      CreateBufferImage(),
      CreateBufferImage(),
      CreateBufferImage(),
      CreateBufferImage()
  };

  _depth = CreateDepthImage();

  _result = CreateRenderTargetImage();

  _shader->SetImage("TColor", _gBuffer.color, _sampler);
  _shader->SetImage("TLocation", _gBuffer.location, _sampler);
  _shader->SetImage("TNormal", _gBuffer.normals, _sampler);
  _shader->SetImage("TRoughMetallic", _gBuffer.roughnessMetallic, _sampler);
  _shader->SetImage("TSpecular", _gBuffer.specular, _sampler);
  _shader->SetImage("TEmissive", _gBuffer.emissive, _sampler);
  _shader->SetBuffer("SceneGlobalBuffer", _sceneGlobalBuffer);
}

std::shared_ptr<AllocatedImage> SceneDeferredDrawer::CreateBufferImage() {
  const auto swapchainExtent = GetWindowDrawer().lock()->
                                                 GetSwapchainExtent();

  const auto imageExtent = vk::Extent3D{
      swapchainExtent.width, swapchainExtent.height, 1};

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eSampled;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;

  auto drawCreateInfo = DrawingSubsystem::MakeImageCreateInfo(
      vk::Format::eR16G16B16A16Sfloat,
      imageExtent,
      drawImageUsages);
  std::shared_ptr<AllocatedImage> newImage = GetDrawer().lock()->GetAllocator().
                                                 lock()->AllocateImage(
                                                     drawCreateInfo,
                                                     VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                                     vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
                                                    vk::ImageViewType::e2D,
                                                    newImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  newImage->view = GetDrawer().lock()->GetVirtualDevice().createImageView(
      drawViewInfo);

  return newImage;
}

std::shared_ptr<AllocatedImage> SceneDeferredDrawer::CreateDepthImage() {

  const auto swapchainExtent = GetWindowDrawer().lock()->
                                                 GetSwapchainExtent();

  const auto imageExtent = vk::Extent3D{
      swapchainExtent.width, swapchainExtent.height, 1};

  auto depthCreateInfo = DrawingSubsystem::MakeImageCreateInfo(
      vk::Format::eD32Sfloat,
      imageExtent,
      vk::ImageUsageFlagBits::eDepthStencilAttachment);

  auto newImage = GetDrawer().lock()->GetAllocator().lock()->
                              AllocateImage(depthCreateInfo,
                                            VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                            vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto depthViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
    vk::ImageViewType::e2D,
    newImage->format, {},
    {vk::ImageAspectFlagBits::eDepth, 0,
     1, 0, 1});

  newImage->view = GetDrawer().lock()->GetVirtualDevice().createImageView(
      depthViewInfo);

  return newImage;
}

std::shared_ptr<AllocatedImage> SceneDeferredDrawer::CreateRenderTargetImage() {
  const auto swapchainExtent = GetWindowDrawer().lock()->
                                                 GetSwapchainExtent();

  const auto imageExtent = vk::Extent3D{
      swapchainExtent.width, swapchainExtent.height, 1};

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  // drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;
  drawImageUsages |= vk::ImageUsageFlagBits::eSampled;

  auto drawCreateInfo = DrawingSubsystem::MakeImageCreateInfo(
      vk::Format::eR16G16B16A16Sfloat,
      imageExtent,
      drawImageUsages);
  std::shared_ptr<AllocatedImage> newImage = GetDrawer().lock()->GetAllocator().
                                                 lock()->AllocateImage(
                                                     drawCreateInfo,
                                                     VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                                     vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
                                                    vk::ImageViewType::e2D,
                                                    newImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  newImage->view = GetDrawer().lock()->GetVirtualDevice().createImageView(
      drawViewInfo);

  return newImage;
}

void SceneDeferredDrawer::Draw(RawFrameData *frameData) {
  auto cmd = frameData->GetCmd();

  const auto scene = GetOwner();

  const auto cameraRef = scene->GetViewTarget().lock()->GetComponentByClass<
    scene::CameraComponent>().lock();

  if (!cameraRef || !cameraRef->IsInitialized()) {
    frameData->GetDrawer()->GetLogger()->Warn(
        "Skipping scene, no active camera");
    return;
  }

  const vk::Extent2D drawExtent = GetWindowDrawer().lock()->
                                                    GetSwapchainExtent();

  TransitionGBuffer(*cmd, vk::ImageLayout::eUndefined,
                    vk::ImageLayout::eColorAttachmentOptimal);

  DrawingSubsystem::TransitionImage(*cmd, _depth->image,
                                    vk::ImageLayout::eUndefined,
                                    vk::ImageLayout::eDepthAttachmentOptimal,
                                    vk::ImageAspectFlagBits::eDepth);

  auto renderingInfo = DrawingSubsystem::MakeRenderingInfo(drawExtent);
  const auto attachments = MakeAttachments();
  renderingInfo.setColorAttachments(attachments);

  vk::ClearValue depthClear;
  depthClear.setDepthStencil({1.f});
  auto depthAttachment = DrawingSubsystem::MakeRenderingAttachment(
      _depth->view, vk::ImageLayout::eDepthAttachmentOptimal, depthClear);

  renderingInfo.setPDepthAttachment(&depthAttachment);

  frameData->GetCmd()->beginRendering(renderingInfo);

  _sceneData.viewMatrix = cameraRef->GetViewMatrix();
  //glm::translate(glm::vec3{ 0,0,-5 }); glm::translate(glm::vec3{ 0,0,15 }); glm::translate(glm::vec3{ 0,0,15 });//
  // camera projection
  _sceneData.projectionMatrix = cameraRef->GetProjection(
      static_cast<float>(drawExtent.width) / static_cast<float>(drawExtent.
        height));

  //some default lighting parameters
  _sceneData.ambientColor = glm::vec4(.1f);
  const auto loc = cameraRef->GetWorldLocation();
  _sceneData.cameraLocation = glm::vec4{loc.x, loc.y, loc.z, 0.0f};

  _sceneData.numLights.x = 0;
  for (const auto &light : scene->GetSceneLights()) {
    if (_sceneData.numLights.x == 1024)
      break;
    if (const auto lightRef = light.lock().get()) {
      _sceneData.lights[static_cast<int>(_sceneData.numLights.x)] = lightRef->
          GetLightInfo();
      _sceneData.numLights.x++;
    }
  }

  auto size = sizeof(_sceneData);
  // Write the buffer
  _sceneGlobalBuffer->Write(_sceneData);

  SceneFrameData drawData(frameData, this);

  // Gather scene
  for (const auto &drawable : scene->GetSceneObjects().clone()) {
    if (auto drawableRef = drawable.lock(); drawableRef->IsInitialized()) {
      drawableRef->Draw(&drawData, {});
    }
  }

  // Draw Scene
  for (const auto &lit : drawData.lit) {
    lit(&drawData);
  }

  cmd->endRendering();

  DrawingSubsystem::TransitionImage(*cmd, _result->image,
                                    vk::ImageLayout::eUndefined,
                                    vk::ImageLayout::eColorAttachmentOptimal);

  TransitionGBuffer(*cmd, vk::ImageLayout::eColorAttachmentOptimal,
                    vk::ImageLayout::eShaderReadOnlyOptimal);

  renderingInfo = DrawingSubsystem::MakeRenderingInfo(drawExtent);
  auto resultAttachment = DrawingSubsystem::MakeRenderingAttachment(
      _result->view, vk::ImageLayout::eColorAttachmentOptimal,vk::ClearValue{{0, 0, 0, 0}});
  renderingInfo.setColorAttachments(resultAttachment);

  cmd->beginRendering(renderingInfo);

  _shader->BindPipeline(frameData);
  _shader->BindSets(frameData);
  cmd->draw(6, 1, 0, 0);

  cmd->endRendering();

  DrawingSubsystem::TransitionImage(*cmd, _result->image,
                                    vk::ImageLayout::eColorAttachmentOptimal,
                                    vk::ImageLayout::eTransferSrcOptimal);
}

void SceneDeferredDrawer::TransitionGBuffer(vk::CommandBuffer cmd,
                                            vk::ImageLayout from,
                                            vk::ImageLayout to) {
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.color->image,
                                    from,
                                    to);
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.emissive->image,
                                    from,
                                    to);
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.location->image,
                                    from,
                                    to);
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.normals->image,
                                    from,
                                    to);
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.specular->image,
                                    from,
                                    to);
  DrawingSubsystem::TransitionImage(cmd, _gBuffer.roughnessMetallic->image,
                                    from,
                                    to);
}


Array<vk::RenderingAttachmentInfo> SceneDeferredDrawer::MakeAttachments() {

  Array<vk::RenderingAttachmentInfo> attachments;
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.color->view, vk::ImageLayout::eColorAttachmentOptimal,
      vk::ClearValue{{0, 0, 0, 0}}));
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.location->view, vk::ImageLayout::eColorAttachmentOptimal,
      vk::ClearValue{{0, 0, 0, 0}}));
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.normals->view, vk::ImageLayout::eColorAttachmentOptimal,
      vk::ClearValue{{0, 0, 0, 0}}));
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.roughnessMetallic->view,
      vk::ImageLayout::eColorAttachmentOptimal, vk::ClearValue{{0, 0, 0, 0}}));
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.specular->view, vk::ImageLayout::eColorAttachmentOptimal,
      vk::ClearValue{{0, 0, 0, 0}}));
  attachments.push(DrawingSubsystem::MakeRenderingAttachment(
      _gBuffer.emissive->view, vk::ImageLayout::eColorAttachmentOptimal,
      vk::ClearValue{{0, 0, 0, 0}}));

  return attachments;
}

Array<vk::Format> SceneDeferredDrawer::GetColorAttachmentFormats() {
  return {vk::Format::eR16G16B16A16Sfloat,
          vk::Format::eR16G16B16A16Sfloat,
          vk::Format::eR16G16B16A16Sfloat,
          vk::Format::eR16G16B16A16Sfloat,
          vk::Format::eR16G16B16A16Sfloat,
          vk::Format::eR16G16B16A16Sfloat};
}

std::weak_ptr<AllocatedImage> SceneDeferredDrawer::GetRenderTarget() {
  return _result;
}

std::shared_ptr<MaterialInstance> SceneDeferredDrawer::CreateMaterialInstance(
    const Array<std::shared_ptr<Shader>> &shaders) {
  auto mat = MaterialBuilder().AddShaders(shaders).SetType(EMaterialType::Lit).AddAttachmentFormats(GetColorAttachmentFormats()).Create();
  mat->SetBuffer("SceneGlobalBuffer",_sceneGlobalBuffer);
  return mat;
}

std::weak_ptr<MaterialInstance> SceneDeferredDrawer::GetDefaultMaterial() {
  return _defaultCheckeredMaterial;
}

void SceneDeferredDrawer::OnDestroy() {
  GetDrawer().lock()->WaitDeviceIdle();
  SceneDrawer::OnDestroy();
}
}
