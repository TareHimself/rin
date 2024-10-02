#pragma once
#include <stack>
#include <vector>
#include <variant>
#include "WidgetDrawCommand.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/widgets/TransformInfo.hpp"
#include "rin/widgets/WidgetsModule.hpp"

struct ClipInfo
{
    uint32_t id;
    Matrix3<float> transform{};
    Vec2<float> size{0.0f};
};

struct RawCommandInfo
{
    Shared<WidgetDrawCommand> command{};
    std::string clipId{};
};

struct CommandInfo
{
    Shared<WidgetDrawCommand> command{};
    std::uint32_t clipMask{};
};

inline constexpr uint32_t RIN_WIDGETS_STENCIL_NO_CLIP_BIT = 0x01;
inline constexpr uint32_t RIN_STENCIL_MAX_CLIP_BIT = 128;


class WidgetDrawCommands
{
    std::vector<ClipInfo> _clips{};
    std::vector<RawCommandInfo> _commands{};
    std::deque<uint32_t> _clipStack{};
    std::string _clipId{};
    std::map<std::string,std::deque<uint32_t>> _uniqueClipStacks{};
public:

    template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<WidgetDrawCommand,T>,WidgetDrawCommands&> Add(TArgs&&... args);
    
    WidgetDrawCommands& Add(const Shared<WidgetDrawCommand>& command);

    WidgetDrawCommands& PushClip(const TransformInfo& transform, const Widget * container);
    WidgetDrawCommands& PushClip(const Matrix3<float>& transform,const Vec2<float>& size);
    WidgetDrawCommands& PopClip();
    
    std::vector<RawCommandInfo>& GetCommands();
    std::vector<ClipInfo>& GetClips();
    std::map<std::string,std::deque<uint32_t>>& GetUniqueClipStacks();
    bool Empty() const;
};

template <typename T, typename ... TArgs>
std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<WidgetDrawCommand, T>, WidgetDrawCommands&>
WidgetDrawCommands::Add(TArgs&&... args)
{
    return Add(newShared<T>(std::forward<TArgs>(args)...));
}
