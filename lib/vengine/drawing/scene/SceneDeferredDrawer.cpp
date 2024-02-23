#include "vengine/drawing/scene/SceneDeferredDrawer.hpp"

#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
#include "vengine/io/io.hpp"
#include "vengine/scene/components/CameraComponent.hpp"
#include "vengine/scene/components/LightComponent.hpp"
#include "vengine/scene/objects/SceneObject.hpp"

namespace vengine::drawing {
void SceneDeferredDrawer::Init(scene::Scene *outer) {
  SceneDrawer::Init(outer);
  vk::SamplerCreateInfo samplerInfo{};
  samplerInfo.setMinFilter(vk::Filter::eNearest);
  samplerInfo.setMagFilter(vk::Filter::eNearest);

  _sampler = GetDrawer().Reserve()->GetVirtualDevice().createSampler(
      samplerInfo);
  _shader = MaterialBuilder().AddShaders({
    Shader::FromSource(
                      io::getRawShaderPath("3d/deferred.vert")),
    Shader::FromSource(
                      io::getRawShaderPath("3d/deferred.frag"))
  })
  .SetType(EMaterialType::Lit)
  .AddAttachmentFormats({vk::Format::eR16G16B16A16Sfloat})
  .Create();
  CreateBuffers();

  AddCleanup(GetWindowDrawer().Reserve()->onResizeScenes,GetWindowDrawer().Reserve()->onResizeScenes.Bind([this]{
    CreateBuffers();
  }));
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

Managed<AllocatedImage> SceneDeferredDrawer::CreateBufferImage() {
  const auto swapchainExtent = GetWindowDrawer().Reserve()->GetSwapchainExtent();

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
  Managed<AllocatedImage> newImage = GetDrawer().Reserve()->GetAllocator().
                                                 Reserve()->AllocateImage(
                                                     drawCreateInfo,
                                                     VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                                     vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
                                                    vk::ImageViewType::e2D,
                                                    newImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  newImage->view = GetDrawer().Reserve()->GetVirtualDevice().createImageView(
      drawViewInfo);

  return newImage;
}

Managed<AllocatedImage> SceneDeferredDrawer::CreateDepthImage() {

  const auto swapchainExtent = GetWindowDrawer().Reserve()->
                                                 GetSwapchainExtent();

  const auto imageExtent = vk::Extent3D{
      swapchainExtent.width, swapchainExtent.height, 1};

  auto depthCreateInfo = DrawingSubsystem::MakeImageCreateInfo(
      vk::Format::eD32Sfloat,
      imageExtent,
      vk::ImageUsageFlagBits::eDepthStencilAttachment);

  auto newImage = GetDrawer().Reserve()->GetAllocator().Reserve()->
                              AllocateImage(depthCreateInfo,
                                            VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                            vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto depthViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
    vk::ImageViewType::e2D,
    newImage->format, {},
    {vk::ImageAspectFlagBits::eDepth, 0,
     1, 0, 1});

  newImage->view = GetDrawer().Reserve()->GetVirtualDevice().createImageView(
      depthViewInfo);

  return newImage;
}

Managed<AllocatedImage> SceneDeferredDrawer::CreateRenderTargetImage() {
  const auto swapchainExtent = GetWindowDrawer().Reserve()->
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
  Managed<AllocatedImage> newImage = GetDrawer().Reserve()->GetAllocator().
                                                 Reserve()->AllocateImage(
                                                     drawCreateInfo,
                                                     VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                                     vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, newImage->image,
                                                    vk::ImageViewType::e2D,
                                                    newImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  newImage->view = GetDrawer().Reserve()->GetVirtualDevice().createImageView(
      drawViewInfo);

  return newImage;
}

void SceneDeferredDrawer::Draw(RawFrameData *frameData) {
  auto cmd = frameData->GetCmd();

  const auto scene = GetOuter();

  const auto cameraRef = scene->GetViewTarget().Reserve()->GetComponentByClass<
    scene::CameraComponent>().Reserve();

  if (!cameraRef && cameraRef->IsInitialized()) {
    frameData->GetDrawer()->GetLogger()->warn(
        "Skipping scene, no active camera");
    return;
  }

  const vk::Extent2D drawExtent = GetWindowDrawer().Reserve()->
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
  for (const auto &light : GetOuter()->GetSceneLights()) {
    if (_sceneData.numLights.x == 1024)
      break;
    if (const auto lightRef = light.Reserve().Get()) {
      _sceneData.lights[static_cast<int>(_sceneData.numLights.x)] = lightRef->
          GetLightInfo();
      _sceneData.numLights.x++;
    }
  }

  // Write the buffer
  const auto mappedData = _sceneGlobalBuffer->GetMappedData();
  const auto sceneUniformData = static_cast<SceneGlobalBuffer *>(mappedData);
  *sceneUniformData = _sceneData;

  SceneFrameData drawData(frameData, this);

  // Gather scene
  for (const auto &drawable : GetOuter()->GetSceneObjects().clone()) {
    if (auto drawableRef = drawable.Reserve(); drawableRef->IsInitialized()) {
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
      _result->view, vk::ImageLayout::eColorAttachmentOptimal);
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

Ref<AllocatedImage> SceneDeferredDrawer::GetRenderTarget() {
  return _result;
}
}
