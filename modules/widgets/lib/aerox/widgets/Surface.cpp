#include "aerox/widgets/Surface.hpp"
#include <ranges>
#include <set>
#include "aerox/graphics/commandBufferUtils.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/widgets/Widget.hpp"
#include "aerox/widgets/Container.hpp"
#include "aerox/widgets/event/CursorDownEvent.hpp"
#include "aerox/widgets/event/CursorMoveEvent.hpp"
#include "aerox/widgets/event/ResizeEvent.hpp"
#include "aerox/widgets/event/ScrollEvent.hpp"
VULKAN_HPP_DEFAULT_DISPATCH_LOADER_DYNAMIC_STORAGE
namespace aerox::widgets
{
    void Surface::DoHover()
    {
        auto cursorPosition = GetCursorPosition();

        auto delta = _lastCursorPosition.has_value() ? cursorPosition - _lastCursorPosition.value() : Vec2<float>(0, 0);

        _lastCursorPosition = cursorPosition;

        auto event = std::make_shared<CursorMoveEvent>(this->GetSharedDynamic<Surface>(), cursorPosition);

        TransformInfo info{this};

        auto size = GetDrawSize();

        if (cursorPosition.x < 0 || cursorPosition.y < 0 || cursorPosition.x > size.x || cursorPosition.y > size.y)
        {
            for (auto& lastHovered : _lastHovered)
            {
                lastHovered->NotifyCursorLeave(event, info.AccountFor(lastHovered));
            }
            _lastHovered.clear();
            return;
        }


        auto previousLastHovered = _lastHovered;
        _lastHovered.clear();

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

        for (auto& widget : previousLastHovered)
        {
            if (!hoveredSet.contains(widget.get()))
            {
                auto widgetTransformInfo = info.AccountFor(widget);
                widget->NotifyCursorLeave(event, widgetTransformInfo);
            }
        }
    }

    std::vector<Shared<Widget>> Surface::GetRootWidgets() const
    {
        return _rootWidgets;
    }

    void Surface::Init()
    {
        CreateImages();
    }

    void Surface::CreateImages()
    {
        auto imageExtent = GetDrawSize().Cast<uint32_t>();
        auto usageFlags = vk::ImageUsageFlagBits::eTransferSrc | vk::ImageUsageFlagBits::eTransferDst |
            vk::ImageUsageFlagBits::eSampled | vk::ImageUsageFlagBits::eColorAttachment;

        auto graphicsModule = GRuntime::Get()->GetModule<graphics::GraphicsModule>();

        _drawImage = graphicsModule->CreateImage({imageExtent.x, imageExtent.y, 1}, vk::Format::eR32G32B32A32Sfloat,
                                                 usageFlags, false, "Widget Surface Main Image");

        _copyImage = graphicsModule->CreateImage({imageExtent.x, imageExtent.y, 1}, vk::Format::eR32G32B32A32Sfloat,
                                                 usageFlags, false, "Widget Surface Main Image");
    }

    void Surface::ClearFocus()
    {
        if (_focusedWidget)
        {
            _focusedWidget->OnFocusLost();
        }

        _focusedWidget.reset();
    }

    bool Surface::RequestFocus(const Shared<Widget>& widget)
    {
        if (_focusedWidget == widget) return true;
        if (!widget->IsHitTestable()) return false;

        ClearFocus();

        _focusedWidget = widget;

        widget->OnFocus();

        return true;
    }

    void Surface::NotifyResize(const Shared<ResizeEvent>& event)
    {
        GRuntime::Get()->GetModule<graphics::GraphicsModule>()->WaitForDeviceIdle();
        _drawImage.reset();
        _copyImage.reset();
        CreateImages();

        for (const auto& rootWidget : GetRootWidgets())
        {
            rootWidget->SetDrawSize(event->size);
        }

        onResize->Invoke(event);
    }

    void Surface::NotifyCursorDown(const Shared<CursorDownEvent>& event)
    {
        // Needs Revision
        // TransformInfo info{this};
        //
        // bool shouldKeepFocus = true;
        // for (auto &rootWidget : std::ranges::reverse_view(GetRootWidgets()))
        // {
        //     if(!rootWidget->IsHitTestable()) continue;
        //     
        //     auto widgetTransformInfo = info.AccountFor(rootWidget);
        //
        //     if(!widgetTransformInfo.IsPointWithin(event->position)) continue;
        //
        //     auto res = rootWidget->NotifyCursorDown(event,widgetTransformInfo);
        //
        //     if(!res) continue;
        //
        //     if(!_focusedWidget) continue;
        //     
        //     while(res->GetParent())
        //     {
        //         if(res != _focusedWidget)
        //         {
        //             res = res->GetParent();
        //             continue;
        //         }
        //
        //         shouldKeepFocus = true;
        //         break;
        //     }
        //
        //     if(!shouldKeepFocus) break;
        //
        //     ClearFocus();
        //     return;
        // }
        //
        // ClearFocus();
    }

    void Surface::NotifyCursorUp(const Shared<CursorUpEvent>& event)
    {
        onCursorUp->Invoke(event);
    }

    void Surface::NotifyCursorMove(const Shared<CursorMoveEvent>& event)
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

    void Surface::NotifyScroll(const Shared<ScrollEvent>& event)
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


    Shared<Widget> Surface::AddChild(const Shared<Widget>& widget)
    {
        widget->SetDrawSize(GetDrawSize().Cast<float>());
        widget->SetRelativeOffset({0, 0});
        widget->NotifyAddedToSurface(this->GetSharedDynamic<Surface>());
        _rootWidgets.push_back(widget);
        _rootWidgetsMap.emplace(widget.get(), widget);
        return widget;
    }

    bool Surface::RemoveChild(const Shared<Widget>& widget)
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

    Shared<graphics::DeviceImage> Surface::GetDrawImage() const
    {
        return _drawImage;
    }

    Shared<graphics::DeviceImage> Surface::GetCopyImage() const
    {
        return _copyImage;
    }

    void Surface::BeginMainPass(graphics::Frame* frame,bool clear)
    {
        using namespace graphics;
        auto size = GetDrawSize().Cast<uint32_t>();
        vk::Extent2D renderExtent{size.x, size.y};
        auto cmd = frame->GetCommandBuffer();
        std::optional<vk::ClearValue> clearColor = clear ? vk::ClearValue{vk::ClearColorValue{0.0f,0.0f,0.0f,0.0f}} : std::optional<vk::ClearValue>{};
        auto attachment = GraphicsModule::MakeRenderingAttachment(GetDrawImage(),vk::ImageLayout::eAttachmentOptimal,clearColor);
        beginRendering(cmd,renderExtent,attachment);
        setInputTopology(cmd, vk::PrimitiveTopology::eTriangleList);
        setPolygonMode(cmd, vk::PolygonMode::eFill);
        disableStencilTest(cmd);
        disableCulling(cmd);
        enableBlendingAlphaBlend(cmd, 0, 1);
        setRenderExtent(cmd,renderExtent);
    }

    void Surface::EndMainPass(graphics::Frame* frame)
    {
        frame->GetCommandBuffer().endRendering();
    }

    void Surface::Draw(graphics::Frame* frame)
    {
        if (!_drawImage || !_copyImage) return;

        const auto cmd = frame->GetCommandBuffer();

        if (_rootWidgets.empty())
        {
            HandleDrawSkipped(frame);
            return;
        }

        DoHover();


        HandleAfterDraw(frame);
    }

    void Surface::OnDispose(bool manual)
    {
        Disposable::OnDispose(manual);
        _drawImage.reset();
        _copyImage.reset();
    }
}
