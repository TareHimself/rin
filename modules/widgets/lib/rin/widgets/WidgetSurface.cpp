#include "rin/widgets/WidgetSurface.hpp"
#include <unordered_map>
#include <ranges>
#include <set>
#include "rin/graphics/commandBufferUtils.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/DeviceBuffer.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include <cstdint>
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/ContainerWidget.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/utils.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/containers/WCRoot.hpp"
#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/ResizeEvent.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"
#include "rin/widgets/graphics/WidgetBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/WidgetBatchInfoPushConstant.hpp"
#include "rin/widgets/graphics/WidgetCustomDrawCommand.hpp"
#include "rin/widgets/graphics/QuadInfo.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

void WidgetSurface::DoHover()
{
    auto cursorPosition = GetCursorPosition();

    auto delta = _lastCursorPosition.has_value() ? cursorPosition - _lastCursorPosition.value() : Vec2<float>(0, 0);

    _lastCursorPosition = cursorPosition;

    auto event = std::make_shared<CursorMoveEvent>(this->GetSharedDynamic<WidgetSurface>(), cursorPosition);

    TransformInfo info{this};

    auto size = GetDrawSize();

    // Store old hover list
    auto oldHoverList = _lastHovered;
    _lastHovered.clear();

    // Build new hover list
    {
        TransformInfo rootTransformInfo{_rootWidget};
        if (rootTransformInfo.IsPointWithin(event->position))
        {
            _rootWidget->NotifyCursorEnter(event, rootTransformInfo, _lastHovered);
        }
    }

    std::set<Widget*> hoveredSet{};
    for (auto& hovered : _lastHovered)
    {
        hoveredSet.emplace(hovered.get());
    }

    Shared<ContainerWidget> lastParent{};
    TransformInfo curTransform = info;
    for (auto& widget : std::ranges::reverse_view(oldHoverList))
    {
        curTransform = lastParent ? lastParent->ComputeChildTransform(widget, curTransform) : curTransform;
        lastParent = std::dynamic_pointer_cast<ContainerWidget>(widget);
        if (!hoveredSet.contains(widget.get()))
        {
            widget->NotifyCursorLeave(event, curTransform);
        }
    }
}

Shared<WCRoot> WidgetSurface::GetRootWidget() const
{
    return _rootWidget;
}

Matrix4<float> WidgetSurface::GetProjection() const
{
    return _projection;
}

void WidgetSurface::Init()
{
    const auto size = GetDrawSize().Cast<float>();
    _projection = static_cast<Matrix4<float>>(glm::ortho(0.0f, size.x, 0.0f, size.y));
    _rootWidget = newShared<WCRoot>();
    _rootWidget->SetOffset(Vec2{0.0f});
    _rootWidget->SetSize(size);
    _rootWidget->SetSurface(this->GetSharedDynamic<WidgetSurface>());
    CreateImages();
}

void WidgetSurface::CreateImages()
{
    auto imageExtent = GetDrawSize().Cast<uint32_t>();
    auto usageFlags = vk::ImageUsageFlagBits::eTransferSrc | vk::ImageUsageFlagBits::eTransferDst |
        vk::ImageUsageFlagBits::eSampled | vk::ImageUsageFlagBits::eColorAttachment;

    auto graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();

    _drawImage = graphicsModule->CreateImage({imageExtent.x, imageExtent.y, 1}, ImageFormat::RGBA32,
                                             usageFlags, false, "Widget Surface Main Image");

    _copyImage = graphicsModule->CreateImage({imageExtent.x, imageExtent.y, 1}, ImageFormat::RGBA32,
                                             usageFlags, false, "Widget Surface Copy Image");

    _stencilImage = graphicsModule->CreateImage({imageExtent.x, imageExtent.y, 1}, ImageFormat::Stencil,
                                                vk::ImageUsageFlagBits::eTransferSrc |
                                                vk::ImageUsageFlagBits::eTransferDst |
                                                vk::ImageUsageFlagBits::eDepthStencilAttachment, false,
                                                "Widget Surface Stencil Image");
}

void WidgetSurface::ClearFocus()
{
    if (_focusedWidget)
    {
        _focusedWidget->OnFocusLost();
    }

    _focusedWidget.reset();
}

bool WidgetSurface::RequestFocus(const Shared<Widget>& widget)
{
    if (_focusedWidget == widget) return true;
    if (!widget->IsHitTestable()) return false;

    ClearFocus();

    _focusedWidget = widget;

    widget->OnFocus();

    return true;
}

void WidgetSurface::NotifyResize(const Shared<ResizeEvent>& event)
{
    GRuntime::Get()->GetModule<GraphicsModule>()->WaitForDeviceIdle();
    _projection = static_cast<Matrix4<float>>(glm::ortho(0.0f, event->size.x, 0.0f, event->size.y));
    _drawImage.reset();
    _copyImage.reset();
    CreateImages();

    if (const auto rootWidget = GetRootWidget())
    {
        rootWidget->SetSize(event->size);
    }

    onResize->Invoke(event);
}

void WidgetSurface::NotifyCursorDown(const Shared<CursorDownEvent>& event)
{
    TransformInfo rootInfo{this};
    bool shouldKeepFocus = false;
    if (const auto root = GetRootWidget())
    {
        for (const auto& slot : std::ranges::reverse_view(root->GetSlots()))
        {
            auto childWidget = slot->GetWidget();
            if (!childWidget->IsHitTestable()) continue;

            auto widgetTransformInfo = rootInfo.AccountFor(childWidget);

            if (!widgetTransformInfo.IsPointWithin(event->position)) continue;

            // The widget that handled the cursor down event
            auto result = childWidget->NotifyCursorDown(event, widgetTransformInfo);

            if (!result) continue;

            if (!_focusedWidget) continue;

            while (result)
            {
                if (result == _focusedWidget)
                {
                    shouldKeepFocus = true;
                    break;
                }

                result = result->GetParent();
            }

            break;
        }
    }


    if (!shouldKeepFocus)
    {
        ClearFocus();
    }
}

void WidgetSurface::NotifyCursorUp(const Shared<CursorUpEvent>& event)
{
    onCursorUp->Invoke(event);
}

void WidgetSurface::NotifyCursorMove(const Shared<CursorMoveEvent>& event)
{
    _lastCursorPosition = event->position;

    if (const auto rootWidget = GetRootWidget())
    {
        if (const TransformInfo info{rootWidget}; info.IsPointWithin(event->position))
        {
            rootWidget->NotifyCursorMove(event, info);
        }
    }
}

void WidgetSurface::NotifyScroll(const Shared<ScrollEvent>& event)
{
    if (const auto rootWidget = GetRootWidget())
    {
        if (const TransformInfo info{rootWidget}; info.IsPointWithin(event->position))
        {
            rootWidget->NotifyScroll(event, info);
        }
    }
}


Shared<Widget> WidgetSurface::AddChild(const Shared<Widget>& widget)
{
    _rootWidget->AddChild(widget);
    return widget;
}

bool WidgetSurface::RemoveChild(const Shared<Widget>& widget)
{
    return _rootWidget->RemoveChild(widget);
}

Shared<DeviceImage> WidgetSurface::GetDrawImage() const
{
    return _drawImage;
}

Shared<DeviceImage> WidgetSurface::GetCopyImage() const
{
    return _copyImage;
}

void WidgetSurface::BeginMainPass(SurfaceFrame* frame, bool clearColor, bool clearStencil)
{
    if (frame->activePass == SurfaceGlobals::MAIN_PASS_ID) return;
    if (!frame->activePass.empty()) EndActivePass(frame);

    auto size = GetDrawSize().Cast<uint32_t>();
    vk::Extent2D renderExtent{size.x, size.y};
    auto cmd = frame->raw->GetCommandBuffer();
    std::optional<vk::ClearValue> drawImageClearColor = clearColor
                                                            ? vk::ClearValue{
                                                                vk::ClearColorValue{0.0f, 0.0f, 0.0f, 0.0f}
                                                            }
                                                            : std::optional<vk::ClearValue>{};
    std::optional<vk::ClearValue> stencilImageClearColor = clearStencil
                                                               ? vk::ClearValue{vk::ClearDepthStencilValue{0.0f, 0}}
                                                               : std::optional<vk::ClearValue>{};
    auto drawImageAttachment = GraphicsModule::MakeRenderingAttachment(GetDrawImage(),
                                                                       vk::ImageLayout::eAttachmentOptimal,
                                                                       drawImageClearColor);
    auto stencilImageAttachment = GraphicsModule::MakeRenderingAttachment(
        _stencilImage, vk::ImageLayout::eDepthStencilAttachmentOptimal, stencilImageClearColor);
    beginRendering(cmd, renderExtent, drawImageAttachment, {}, stencilImageAttachment);
    disableVertexInput(cmd);
    disableRasterizerDiscard(cmd);
    disableMultiSampling(cmd);
    disableCulling(cmd);
    disableDepthTest(cmd);
    setInputTopology(cmd, vk::PrimitiveTopology::eTriangleList);
    setPolygonMode(cmd, vk::PolygonMode::eFill);
    enableBlendingAlphaBlend(cmd, 0, 1);
    setRenderExtent(cmd, renderExtent);
    cmd.setStencilTestEnable(true);
    auto faceMask = vk::StencilFaceFlagBits::eFrontAndBack;
    cmd.setStencilReference(faceMask, 255);
    cmd.setStencilCompareMask(faceMask, 0x01);
    cmd.setStencilWriteMask(faceMask, 0x01);
    cmd.setStencilOp(faceMask, vk::StencilOp::eKeep, vk::StencilOp::eKeep, vk::StencilOp::eKeep, vk::CompareOp::eNever);
    frame->activePass = SurfaceGlobals::MAIN_PASS_ID;
}

void WidgetSurface::EndActivePass(SurfaceFrame* frame)
{
    if (frame->activePass.empty()) return;
    auto cmd = frame->raw->GetCommandBuffer();
    cmd.endRendering();
    frame->activePass.clear();
}

void WidgetSurface::Draw(Frame* frame)
{
    if (!_drawImage || !_copyImage) return;

    const auto cmd = frame->GetCommandBuffer();

    if (_rootWidget->GetUsedSlots() == 0)
    {
        HandleDrawSkipped(frame);
        return;
    }

    DoHover();

    {
        SurfaceFrame surfaceFrame{this, frame, ""};
        auto surfaceFramePtr = &surfaceFrame;
        std::vector<SurfaceFinalDrawCommand> finalDrawCommands{};
        CollectCommands(surfaceFramePtr, finalDrawCommands);

        if (finalDrawCommands.empty())
        {
            HandleDrawSkipped(frame);
            return;
        }

        HandleBeforeDraw(frame);

        BeginMainPass(surfaceFramePtr, true, true);

        auto graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();
        auto limits = graphicsModule->GetPhysicalDevice().getProperties().limits;
        auto minBufferOffsetAlignment = limits.minUniformBufferOffsetAlignment;
        uint64_t memoryNeeded = 0;
        uint64_t finalMemoryNeeded = 0;
        for (auto& drawCommand : finalDrawCommands)
        {
            if (drawCommand.type == SurfaceFinalDrawCommand::Type::BatchedDraw)
            {
                finalMemoryNeeded = drawCommand.size.value();
                memoryNeeded += finalMemoryNeeded;
                auto dist = memoryNeeded % minBufferOffsetAlignment;
                // To Account for the offset we will need later
                memoryNeeded += dist > 0 ? minBufferOffsetAlignment - dist : 0;
            }
        }


        auto widgetsModule = GRuntime::Get()->GetModule<WidgetsModule>();
        auto batchShader = widgetsModule->GetBatchShader();
        auto batchBufferResource = batchShader->resources.at("batch_info");
        auto pushConstantResource = batchShader->pushConstants.at("push");

        uint64_t offset = 0;
        bool isWritingStencil = false;
        bool isComparingStencil = false;
        bool isBatching = false;
        finalMemoryNeeded = finalMemoryNeeded % batchBufferResource.size;
        memoryNeeded += batchBufferResource.size - finalMemoryNeeded;

        vk::StencilFaceFlags faceFlags = vk::StencilFaceFlagBits::eFrontAndBack;


        //vk::PhysicalDeviceLimits::minUniformBufferOffsetAlignment

        auto batchBuffer = graphicsModule->GetAllocator()->NewUniformBuffer(memoryNeeded, true, "Widget Batch Buffer");

        for (int i = 0; i < finalDrawCommands.size(); i++)
        {
            auto& command = finalDrawCommands.at(i);

            switch (command.type)
            {
            case SurfaceFinalDrawCommand::Type::None:
                break;
            case SurfaceFinalDrawCommand::Type::ClipDraw:
                {
                    BeginMainPass(surfaceFramePtr);

                    if (!isWritingStencil)
                    {
                        cmd.setStencilOp(faceFlags, vk::StencilOp::eKeep, vk::StencilOp::eReplace, vk::StencilOp::eKeep,
                                         vk::CompareOp::eAlways);
                        SetColorWriteMask(frame, vk::ColorComponentFlags());
                        isWritingStencil = true;
                        isComparingStencil = false;
                    }

                    cmd.setStencilWriteMask(faceFlags, command.mask.value());
                    WriteStencil(frame, command.clipInfo->transform, command.clipInfo->size);
                }
                break;
            case SurfaceFinalDrawCommand::Type::ClipClear:
                {
                    BeginMainPass(surfaceFramePtr);

                    vk::ClearAttachment clearAttachment{
                        vk::ImageAspectFlagBits::eStencil, {},
                        vk::ClearValue{vk::ClearColorValue{0.0f, 0.0f, 0.0f, 0.0f}}
                    };
                    auto extent = _stencilImage->GetExtent();
                    vk::ClearRect clearRect{
                        vk::Rect2D{vk::Offset2D{0, 0}, vk::Extent2D{extent.width, extent.height}}, 0, 1
                    };
                    cmd.clearAttachments(clearAttachment, clearRect);
                }
                break;
            case SurfaceFinalDrawCommand::Type::BatchedDraw:
            case SurfaceFinalDrawCommand::Type::CustomDraw:
                {
                    BeginMainPass(surfaceFramePtr);

                    if (!isComparingStencil)
                    {
                        enableStencilCompare(cmd, command.mask.value(), vk::CompareOp::eNotEqual);
                        SetColorWriteMask(
                            frame,
                            vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG |
                            vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
                        isWritingStencil = false;
                        isComparingStencil = true;
                    }
                    else
                    {
                        cmd.setStencilCompareMask(faceFlags, command.mask.value());
                    }

                    if (command.type == SurfaceFinalDrawCommand::Type::CustomDraw)
                    {
                        command.custom->Run(surfaceFramePtr);
                    }
                    else
                    {
                        if (!isBatching)
                        {
                            if (batchShader->Bind(cmd, true))
                            {
                                isBatching = true;
                            }
                        }

                        if (isBatching)
                        {
                            auto batchSetLayout = batchShader->GetDescriptorSetLayouts().at(batchBufferResource.set);
                            auto batchPipelineLayout = batchShader->GetPipelineLayout();
                            
                            auto size = GetDrawSize().Cast<float>();
                            Vec4<float> viewport{0.0f, 0.0f, size.x, size.y};
                            for (auto j = 0; j < command.quads->size(); j += RIN_WIDGET_MAX_BATCH)
                            {
                                auto set = frame->GetAllocator()->Allocate(batchSetLayout);

                                auto totalQuads = std::min(RIN_WIDGET_MAX_BATCH,
                                                           static_cast<int>(command.quads->size() - j));

                                const auto writeSize = totalQuads * sizeof(QuadInfo);

                                batchBuffer->Write(command.quads->data() + j, writeSize, offset);

                                set->WriteBuffer(batchBufferResource.binding, batchBufferResource.type, batchBuffer,
                                                 offset, RIN_WIDGET_MAX_BATCH * sizeof(QuadInfo));

                                offset += writeSize;

                                // Offset must be a multiple of minBufferOffsetAlignment
                                auto dist = offset % minBufferOffsetAlignment;
                                offset += dist > 0 ? minBufferOffsetAlignment - dist : 0;

                                std::vector<vk::DescriptorSet> sets = {
                                    graphicsModule->GetResourceManager()->GetDescriptorSet(), set->GetInternalSet()
                                };

                                cmd.bindDescriptorSets(vk::PipelineBindPoint::eGraphics, batchPipelineLayout, 0, sets,
                                                       {});

                                WidgetBatchInfoPushConstant pushConstant{viewport, GetProjection()};

                                cmd.pushConstants(batchPipelineLayout, pushConstantResource.stages, 0,
                                                  pushConstantResource.size, &pushConstant);

                                cmd.draw(6, totalQuads, 0, 0);
                            }
                            continue;
                        }
                    }
                }
                break;
            }

            isBatching = false;
        }

        if (!surfaceFrame.activePass.empty())
        {
            EndActivePass(surfaceFramePtr);
        }
    }

    HandleAfterDraw(frame);
}

void WidgetSurface::CollectCommands(SurfaceFrame* frame, std::vector<SurfaceFinalDrawCommand>& finalDrawCommands)
{
    TransformInfo transform{_rootWidget};
    WidgetDrawCommands drawCommands{};

    _rootWidget->Collect(transform, drawCommands);

    if (drawCommands.Empty())
    {
        return;
    }

    auto& clips = drawCommands.GetClips();
    auto rawCommands = drawCommands.GetCommands();
    if (clips.empty())
    {
        std::vector<CommandInfo> commands(rawCommands.size());
        std::ranges::transform(rawCommands.begin(), rawCommands.end(), commands.begin(),
                               [](const RawCommandInfo& rawInfo)
                               {
                                   return CommandInfo{rawInfo.command, 0x01};
                               });
        DrawCommandsToFinalCommands(frame, commands, finalDrawCommands);
    }
    else
    {
        auto faceFlags = vk::StencilFaceFlagBits::eFrontAndBack;
        auto& uniqueClipStacks = drawCommands.GetUniqueClipStacks();
        std::unordered_map<std::string, uint32_t> computedClipStacks{};
        std::vector<CommandInfo> pendingCommands{};
        std::uint32_t currentMask = 0x01;
        for (auto i = 0; i < rawCommands.size(); i++)
        {
            if (currentMask == 128)
            {
                DrawCommandsToFinalCommands(frame, pendingCommands, finalDrawCommands);
                pendingCommands.clear();
                computedClipStacks.clear();
                currentMask = 0x01;
                finalDrawCommands.emplace_back().type = SurfaceFinalDrawCommand::Type::ClipClear;
            }

            if (auto& rawCommand = rawCommands.at(i); computedClipStacks.contains(rawCommand.clipId))
            {
                pendingCommands.emplace_back(rawCommand.command, computedClipStacks[rawCommand.clipId]);
            }
            else
            {
                currentMask <<= 1;

                for (auto& clipId : uniqueClipStacks.at(rawCommand.clipId))
                {
                    auto clip = clips.at(clipId);
                    auto& fCmd = finalDrawCommands.emplace_back();
                    fCmd.type = SurfaceFinalDrawCommand::Type::ClipDraw;
                    fCmd.clipInfo = clip;
                    fCmd.mask = currentMask;
                }
                computedClipStacks.emplace(rawCommand.clipId, currentMask);
                pendingCommands.emplace_back(rawCommand.command, currentMask);
            }
        }

        if (!pendingCommands.empty())
        {
            DrawCommandsToFinalCommands(frame, pendingCommands, finalDrawCommands);
            pendingCommands.clear();
        }
    }
}

void WidgetSurface::DrawCommandsToFinalCommands(SurfaceFrame* frame, const std::vector<CommandInfo>& drawCommands,
                                                std::vector<SurfaceFinalDrawCommand>& finalDrawCommands)
{
    std::vector<QuadInfo> pendingQuads{};
    uint32_t currentClipMask = bitshift(0); // First bit is reserved for draws with no clipping
    for (auto& [drawCommand,clipMask] : drawCommands)
    {
        if (currentClipMask != clipMask)
        {
            if (!pendingQuads.empty())
            {
                auto& cmd = finalDrawCommands.emplace_back();
                cmd.SetQuads(pendingQuads, currentClipMask);
                pendingQuads = {};
            }
            currentClipMask = clipMask;
        }

        switch (drawCommand->GetType())
        {
        case WidgetDrawCommand::Type::Batched:
            {
                if (auto batched = std::dynamic_pointer_cast<WidgetBatchedDrawCommand>(drawCommand))
                {
                    auto quads = batched->ComputeQuads();
                    pendingQuads.insert(pendingQuads.end(), quads.begin(), quads.end());
                }
            }
            break;
        case WidgetDrawCommand::Type::Custom:
            {
                if (auto custom = std::dynamic_pointer_cast<WidgetCustomDrawCommand>(drawCommand))
                {
                    {
                        auto& cmd = finalDrawCommands.emplace_back();
                        cmd.SetQuads(pendingQuads, currentClipMask);
                    }

                    {
                        auto& cmd = finalDrawCommands.emplace_back();
                        cmd.type = SurfaceFinalDrawCommand::Type::CustomDraw;
                        cmd.custom = custom;
                        cmd.mask = currentClipMask;
                    }


                    custom->Run(frame);
                }
            }
            break;
        }
    }

    if (!pendingQuads.empty())
    {
        auto& cmd = finalDrawCommands.emplace_back();
        cmd.SetQuads(pendingQuads, currentClipMask);
    }
}

bool WidgetSurface::WriteStencil(const Frame* frame, const std::vector<WidgetStencilClip>& clips)
{
    auto cmd = frame->GetCommandBuffer();
    if (auto stencilShader = WidgetsModule::Get()->GetStencilShader())
    {
        if (stencilShader->Bind(cmd, true))
        {
            for (auto i = 0; i < clips.size(); i += RIN_WIDGETS_MAX_STENCIL_CLIP)
            {
                WidgetStencilBuffer buff{};
                buff.projection = GetProjection();
                auto totalQuads = std::min(RIN_WIDGETS_MAX_STENCIL_CLIP, static_cast<int>(clips.size() - i));
                memcpy(&buff.clips, clips.data() + i, totalQuads * sizeof(WidgetStencilClip));
                auto set = frame->GetAllocator()->Allocate(stencilShader->GetDescriptorSetLayouts().at(0));
                auto dataBuffer = GraphicsModule::Get()->GetAllocator()->NewResourceBuffer(
                    stencilShader->resources.at("stencil_info"));
                dataBuffer->Write(buff);
                cmd.bindDescriptorSets(vk::PipelineBindPoint::eGraphics, stencilShader->GetPipelineLayout(), 0,
                                       set->GetInternalSet(), {});
                cmd.draw(totalQuads * 6, 1, 0, 0);
            }
            return true;
        }
    }

    return false;
}

bool WidgetSurface::WriteStencil(const Frame* frame, const Matrix3<float>& transform, const Vec2<float>& size)
{
    auto cmd = frame->GetCommandBuffer();
    if (auto stencilShader = WidgetsModule::Get()->GetStencilShader())
    {
        if (stencilShader->Bind(cmd, true))
        {
            SingleStencilDrawPush push{GetProjection(), transform, size};
            auto pushConstantResource = stencilShader->pushConstants.begin()->second;
            cmd.pushConstants(stencilShader->GetPipelineLayout(), pushConstantResource.stages, 0,
                              pushConstantResource.size, &push);
            cmd.draw(6, 1, 0, 0);
            return true;
        }
    }

    return false;
}

void WidgetSurface::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);
    _drawImage->Dispose();
    _copyImage->Dispose();
}

void WidgetSurface::SetColorWriteMask(const Frame* frame, const vk::ColorComponentFlags& flags)
{
    frame->GetCommandBuffer().setColorWriteMaskEXT(0, flags, GraphicsModule::GetDispatchLoader());
}
