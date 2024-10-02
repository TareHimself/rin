#pragma once
#include "rin/core/Disposable.hpp"
#include "rin/core/delegates/DelegateList.hpp"
#include "rin/core/math/Vec2.hpp"
#include "rin/graphics/DeviceImage.hpp"
#include <optional>

#include "rin/graphics/Frame.hpp"
#include "graphics/QuadInfo.hpp"
#include "rin/core/math/Matrix4.hpp"

struct CommandInfo;
struct WidgetStencilClip;
class WidgetDrawCommand;
struct RawCommandInfo;
struct SurfaceFrame;
class Widget;
class CursorDownEvent;
    class ResizeEvent;
    class CursorMoveEvent;
    class CursorUpEvent;
    class ScrollEvent;

inline int RIN_WIDGETS_STENCIL_MAX_SHIFT = 8;
namespace SurfaceGlobals
{
   inline std::string MAIN_PASS_ID = "MAIN";
}
    class WidgetSurface : public Disposable
    {

        Shared<DeviceImage> _copyImage{};
        Shared<DeviceImage> _drawImage{};
        Shared<DeviceImage> _stencilImage{};
        std::optional<Vec2<float>> _lastCursorPosition{};
        std::unordered_map<Widget *,Shared<Widget>> _rootWidgetsMap{};
        std::vector<Shared<Widget>> _rootWidgets{};
        Shared<Widget> _focusedWidget{};
        std::vector<Shared<Widget>> _lastHovered{};
        Matrix4<float> _projection{1.0f};

        void DoHover();
        
    public:
    
        std::vector<Shared<Widget>> GetRootWidgets() const;
        DEFINE_DELEGATE_LIST(onCursorDown,const Shared<CursorDownEvent>&)
        DEFINE_DELEGATE_LIST(onCursorUp,const Shared<CursorUpEvent>&)
        DEFINE_DELEGATE_LIST(onCursorMove,const Shared<CursorMoveEvent>&)
        DEFINE_DELEGATE_LIST(onResize,const Shared<ResizeEvent>&)
        DEFINE_DELEGATE_LIST(onScroll,const Shared<ScrollEvent>&)

        Matrix4<float> GetProjection() const;
        virtual void Init();

        virtual Vec2<int> GetDrawSize() const = 0;
        virtual Vec2<float> GetCursorPosition() const = 0;

        virtual void CreateImages();

        virtual void ClearFocus();

        virtual bool RequestFocus(const Shared<Widget>& widget);

        virtual void NotifyResize(const Shared<ResizeEvent>& event);
        virtual void NotifyCursorDown(const Shared<CursorDownEvent>& event);
        virtual void NotifyCursorUp(const Shared<CursorUpEvent>& event);
        virtual void NotifyCursorMove(const Shared<CursorMoveEvent>& event);
        virtual void NotifyScroll(const Shared<ScrollEvent>& event);

        virtual Shared<Widget> AddChild(const Shared<Widget>& widget);

        virtual bool RemoveChild(const Shared<Widget>& widget);

        Shared<DeviceImage> GetDrawImage() const;
        Shared<DeviceImage> GetCopyImage() const;

        virtual void BeginMainPass(SurfaceFrame * frame,bool clearColor = false,bool clearStencil = false);
        virtual void EndActivePass(SurfaceFrame * frame);

        virtual void DrawBatches(SurfaceFrame * frame,std::vector<QuadInfo>& quads);
        virtual void Draw(Frame * frame);

        virtual void DrawCommands(SurfaceFrame * frame,const std::vector<CommandInfo>& drawCommands);

        virtual void HandleDrawSkipped(Frame* frame) = 0;
        virtual void HandleBeforeDraw(Frame* frame) = 0;
        virtual void HandleAfterDraw(Frame* frame) = 0;

        bool WriteStencil(const Frame * frame,const std::vector<WidgetStencilClip>& clips);

        bool WriteStencil(const Frame * frame,const Matrix3<float>& transform,const Vec2<float>& size);

        void OnDispose(bool manual) override;

    void SetColorWriteMask(const Frame * frame,const vk::ColorComponentFlags& flags);

        template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<Widget,T>,Shared<T>> AddChild(TArgs&&... args);

        
    };

    template <typename T, typename ... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T>, Shared<T>> WidgetSurface::
    AddChild(TArgs&&... args)
    {
        auto w = newShared<T>(std::forward<TArgs>(args)...);
        AddChild(w);
        return w;
    }
