#include "rin/widgets/WidgetSurface.hpp"
#include <unordered_map>
#include <iostream>
#include <ranges>
#include <set>
#include "rin/graphics/commandBufferUtils.hpp"
#include "rin/core/GRuntime.hpp"
#include "rin/core/utils.hpp"
#include "rin/graphics/DeviceBuffer.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/graphics/ResourceManager.hpp"
#include "rin/widgets/utils.hpp"
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/ContainerWidget.hpp"
#include "rin/widgets/WidgetsModule.hpp"
#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/ResizeEvent.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"
#include "rin/widgets/graphics/WidgetBatchedDrawCommand.hpp"
#include "rin/widgets/graphics/BatchInfo.hpp"
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
    for (auto& rootWidget : std::ranges::reverse_view(_rootWidgets))
    {
        if (!rootWidget->IsHitTestable()) continue;

        auto widgetTransformInfo = info.AccountFor(rootWidget);

        if (!widgetTransformInfo.IsPointWithin(event->position)) continue;

        rootWidget->NotifyCursorEnter(event, widgetTransformInfo, _lastHovered);

        break;
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

std::vector<Shared<Widget>> WidgetSurface::GetRootWidgets() const
{
    return _rootWidgets;
}

Matrix4<float> WidgetSurface::GetProjection() const
{
    return _projection;
}

void WidgetSurface::Init()
{
    auto size = GetDrawSize().Cast<float>();
    _projection = static_cast<Matrix4<float>>(glm::ortho(0.0f, size.x, 0.0f, size.y));
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
                                             usageFlags, false, "Widget Surface Main Image");

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

    for (const auto& rootWidget : GetRootWidgets())
    {
        rootWidget->SetSize(event->size);
    }

    onResize->Invoke(event);
}

void WidgetSurface::NotifyCursorDown(const Shared<CursorDownEvent>& event)
{
    TransformInfo rootInfo{this};
    bool shouldKeepFocus = false;
    for (auto& rootWidget : std::ranges::reverse_view(GetRootWidgets()))
    {
        if (!rootWidget->IsHitTestable()) continue;

        auto widgetTransformInfo = rootInfo.AccountFor(rootWidget);

        if (!widgetTransformInfo.IsPointWithin(event->position)) continue;

        // The widget that handled the cursor down event
        auto result = rootWidget->NotifyCursorDown(event, widgetTransformInfo);

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

    TransformInfo info{this};

    for (auto& rootWidget : GetRootWidgets())
    {
        if (!rootWidget->IsHitTestable()) continue;

        auto widgetTransformInfo = info.AccountFor(rootWidget);

        if (!widgetTransformInfo.IsPointWithin(event->position)) continue;

        if (!rootWidget->NotifyCursorMove(event, widgetTransformInfo)) continue;

        break;
    }
}

void WidgetSurface::NotifyScroll(const Shared<ScrollEvent>& event)
{
    TransformInfo info{this};

    for (auto& rootWidget : GetRootWidgets())
    {
        if (!rootWidget->IsHitTestable()) continue;

        auto widgetTransformInfo = info.AccountFor(rootWidget);

        if (!widgetTransformInfo.IsPointWithin(event->position)) continue;

        if (!rootWidget->NotifyScroll(event, widgetTransformInfo)) continue;

        break;
    }
}


Shared<Widget> WidgetSurface::AddChild(const Shared<Widget>& widget)
{
    widget->NotifyAddedToSurface(this->GetSharedDynamic<WidgetSurface>());
    widget->SetOffset({0, 0});
    widget->SetSize(GetDrawSize().Cast<float>());
    _rootWidgets.push_back(widget);
    _rootWidgetsMap.emplace(widget.get(), widget);
    return widget;
}

bool WidgetSurface::RemoveChild(const Shared<Widget>& widget)
{
    if (_rootWidgetsMap.contains(widget.get())) return false;

    _rootWidgetsMap.erase(widget.get());

    for (auto i = 0; i < _rootWidgets.size(); i++)
    {
        if (_rootWidgets[i].get() == widget.get())
        {
            _rootWidgets.erase(_rootWidgets.begin() + i);
            break;
        }
    }

    return true;
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
    if(frame->activePass == SurfaceGlobals::MAIN_PASS_ID) return;
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

void WidgetSurface::DrawBatches(SurfaceFrame* frame, std::vector<QuadInfo>& quads)
{
    if (quads.empty()) return;

    if (frame->activePass != SurfaceGlobals::MAIN_PASS_ID)
    {
        EndActivePass(frame);
        BeginMainPass(frame);
    }

    auto graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();
    auto widgetsModule = GRuntime::Get()->GetModule<WidgetsModule>();
    auto shader = widgetsModule->GetBatchShader();
    auto cmd = frame->raw->GetCommandBuffer();

    if (shader->Bind(cmd, true))
    {
        auto resource = shader->resources.at("batch_info");
        auto setLayout = shader->GetDescriptorSetLayouts().at(resource.set);
        auto pipelineLayout = shader->GetPipelineLayout();
        auto size = GetDrawSize().Cast<float>();
        Vec4<float> viewport{0.0f, 0.0f, size.x, size.y};
        for (auto i = 0; i < quads.size(); i += BatchInfo::MAX_BATCH)
        {
            auto set = frame->raw->GetAllocator()->Allocate(setLayout);
            auto dataBuffer = graphicsModule->GetAllocator()->NewResourceBuffer(resource);
            auto totalQuads = std::min(BatchInfo::MAX_BATCH, static_cast<int>(quads.size() - i));
            BatchInfo data{viewport, GetProjection()};
            memcpy(&data.quads, quads.data() + i, totalQuads * sizeof(QuadInfo));
            dataBuffer->Write(data);
            set->WriteBuffer(resource.binding, resource.type, dataBuffer);
            std::vector<vk::DescriptorSet> sets = {
                graphicsModule->GetResourceManager()->GetDescriptorSet(), set->GetInternalSet()
            };
            cmd.bindDescriptorSets(vk::PipelineBindPoint::eGraphics, pipelineLayout, 0, sets, {});
            if (!dataBuffer || dataBuffer->IsDisposed())
            {
                throw std::runtime_error("YO");
            }
            cmd.draw(totalQuads * 6, 1, 0, 0);
        }
    }

    quads = {};
}

void WidgetSurface::Draw(Frame* frame)
{
    if (!_drawImage || !_copyImage) return;

    const auto cmd = frame->GetCommandBuffer();

    if (_rootWidgets.empty())
    {
        HandleDrawSkipped(frame);
        return;
    }

    DoHover();

    {
        std::vector<Shared<Widget>> widgets = _rootWidgets;
        TransformInfo transform{this};
        WidgetDrawCommands drawCommands{};
        for (auto& widget : widgets)
        {
            widget->Collect(transform, drawCommands);
        }

        if (drawCommands.Empty())
        {
            HandleDrawSkipped(frame);
            return;
        }

        HandleBeforeDraw(frame);

        SurfaceFrame surfFrame{this, frame, ""};
        auto &clips = drawCommands.GetClips();
        auto rawCommands = drawCommands.GetCommands();
        BeginMainPass(&surfFrame,true,true);
        if(clips.empty())
        {
            std::vector<CommandInfo> commands(rawCommands.size());
            std::ranges::transform(rawCommands.begin(),rawCommands.end(),commands.begin(),[](const RawCommandInfo& rawInfo)
            {
                return CommandInfo{rawInfo.command,0x01};
            });
            DrawCommands(&surfFrame,commands);
        }
        else
        {
            auto faceFlags = vk::StencilFaceFlagBits::eFrontAndBack;
            auto &uniqueClipStacks = drawCommands.GetUniqueClipStacks();
            std::unordered_map<std::string,uint32_t> computedClipStacks{};
            std::vector<CommandInfo> pendingCommands{};
            std::uint32_t currentMask = 0x01;
            BeginMainPass(&surfFrame);
            cmd.setStencilOp(faceFlags,vk::StencilOp::eKeep,vk::StencilOp::eReplace,vk::StencilOp::eKeep,vk::CompareOp::eAlways);
            SetColorWriteMask(frame,vk::ColorComponentFlags());
            for(auto i = 0; i < rawCommands.size(); i++)
            {
                if(currentMask == 128)
                {
                    DrawCommands(&surfFrame,pendingCommands);
                    pendingCommands.clear();
                    computedClipStacks.clear();
                    currentMask = 0x01;
                    

                    // if(surfFrame.activePass == SurfaceGlobals::MAIN_PASS_ID)
                    // {
                    //     EndActivePass(&surfFrame);
                    // }
                    
                    //BeginMainPass(&surfFrame,false,true);
                    
                    //cmd.setStencilOp(faceFlags,vk::StencilOp::eKeep,vk::StencilOp::eReplace,vk::StencilOp::eKeep,vk::CompareOp::eAlways);
                    vk::ClearAttachment clearAttachment{vk::ImageAspectFlagBits::eStencil,{},vk::ClearValue{vk::ClearColorValue{0.0f,0.0f,0.0f,0.0f}}};
                    auto extent = _stencilImage->GetExtent();
                    vk::ClearRect clearRect{vk::Rect2D{vk::Offset2D{0,0},vk::Extent2D{extent.width,extent.height}},0,1};
                    cmd.clearAttachments(clearAttachment,clearRect);
                    cmd.setStencilOp(faceFlags,vk::StencilOp::eKeep,vk::StencilOp::eReplace,vk::StencilOp::eKeep,vk::CompareOp::eAlways);
                    SetColorWriteMask(frame,vk::ColorComponentFlags());
                }

                auto &rawCommand = rawCommands.at(i);
                if(computedClipStacks.contains(rawCommand.clipId))
                {
                    pendingCommands.emplace_back(rawCommand.command,computedClipStacks[rawCommand.clipId]);
                }
                else
                {
                    currentMask <<= 1;
                    cmd.setStencilWriteMask(faceFlags,currentMask);
                    //cmd.setStencilCompareMask(faceFlags,currentMask);
                    
                    for(auto &clipId : uniqueClipStacks.at(rawCommand.clipId))
                    {
                        auto clip = clips.at(clipId);
                        WriteStencil(frame,clip.transform,clip.size);
                    }
                    computedClipStacks.emplace(rawCommand.clipId,currentMask);
                    pendingCommands.emplace_back(rawCommand.command,currentMask);
                }
            }

            if(!pendingCommands.empty())
            {
                DrawCommands(&surfFrame,pendingCommands);
                pendingCommands.clear();
            }
        }

        if (!surfFrame.activePass.empty())
        {
            EndActivePass(&surfFrame);
        }
    }

    HandleAfterDraw(frame);
}

void WidgetSurface::DrawCommands(SurfaceFrame* frame, const std::vector<CommandInfo>& drawCommands)
{
    BeginMainPass(frame);
    
    std::vector<QuadInfo> pendingQuads{};
    uint32_t currentClipMask = bitshift(0); // First bit is reserved for draws with no clipping
    auto cmd = frame->raw->GetCommandBuffer();
    SetColorWriteMask(frame->raw,vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG | vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
    enableStencilCompare(cmd,currentClipMask,vk::CompareOp::eNotEqual);
    for (auto& [drawCommand,clipMask] : drawCommands)
    {
        if(currentClipMask != clipMask)
        {
            DrawBatches(frame,pendingQuads);
            currentClipMask = clipMask;
            BeginMainPass(frame);
            cmd.setStencilCompareMask(vk::StencilFaceFlagBits::eFrontAndBack,currentClipMask);
            SetColorWriteMask(frame->raw,vk::ColorComponentFlagBits::eR | vk::ColorComponentFlagBits::eG | vk::ColorComponentFlagBits::eB | vk::ColorComponentFlagBits::eA);
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
                    DrawBatches(frame,pendingQuads);
                    custom->Run(frame);
                }
            }
            break;
        }
    }

    if (!pendingQuads.empty())
    {
        DrawBatches(frame, pendingQuads);
    }
}

bool WidgetSurface::WriteStencil(const Frame * frame, const std::vector<WidgetStencilClip>& clips)
{
    auto cmd = frame->GetCommandBuffer();
    if(auto stencilShader = WidgetsModule::Get()->GetStencilShader())
    {
        if(stencilShader->Bind(cmd,true))
        {
            
            for(auto i = 0; i < clips.size(); i += RIN_WIDGETS_MAX_STENCIL_CLIP)
            {
                WidgetStencilBuffer buff{};
                buff.projection = GetProjection();
                auto totalQuads = std::min(RIN_WIDGETS_MAX_STENCIL_CLIP, static_cast<int>(clips.size() - i));
                memcpy(&buff.clips, clips.data() + i, totalQuads * sizeof(WidgetStencilClip));
                auto set = frame->GetAllocator()->Allocate(stencilShader->GetDescriptorSetLayouts().at(0));
                auto dataBuffer = GraphicsModule::Get()->GetAllocator()->NewResourceBuffer(stencilShader->resources.at("stencil_info"));
                dataBuffer->Write(buff);
                cmd.bindDescriptorSets(vk::PipelineBindPoint::eGraphics,stencilShader->GetPipelineLayout(), 0,set->GetInternalSet(), {});
                cmd.draw(totalQuads * 6,1,0,0);
            }
            return true;
        }
    }
    
    return false;
}

bool WidgetSurface::WriteStencil(const Frame* frame, const Matrix3<float>& transform, const Vec2<float>& size)
{
    auto cmd = frame->GetCommandBuffer();
    if(auto stencilShader = WidgetsModule::Get()->GetStencilShader())
    {
        if(stencilShader->Bind(cmd,true))
        {
            SingleStencilDrawPush push{GetProjection(),transform,size};
            auto pushConstantResource = stencilShader->pushConstants.begin()->second;
            cmd.pushConstants(stencilShader->GetPipelineLayout(),pushConstantResource.stages,0,pushConstantResource.size,&push);
            cmd.draw(6,1,0,0);
            return true;
        }
    }
    
    return false;
}

void WidgetSurface::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);
    _drawImage.reset();
    _copyImage.reset();
}

void WidgetSurface::SetColorWriteMask(const Frame* frame, const vk::ColorComponentFlags& flags)
{
    frame->GetCommandBuffer().setColorWriteMaskEXT(0,flags,GraphicsModule::GetDispatchLoader());
}
