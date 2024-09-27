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
    StencilPushConstants pushConstants;
};

struct CommandInfo
{
    Shared<WidgetDrawCommand> command{};
    std::stack<uint32_t> clipStack{};
};


class WidgetDrawCommands
{
    std::vector<ClipInfo> _clips{};
    std::vector<CommandInfo> _commands{};
    std::stack<uint32_t> _clipStack{};
    std::map<uint32_t,uint32_t> _breaks{};
public:

    template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<WidgetDrawCommand,T>,WidgetDrawCommands&> Add(TArgs&&... args);

    
    WidgetDrawCommands& Add(const Shared<WidgetDrawCommand>& command);

    WidgetDrawCommands& PushClip(const TransformInfo& transform, const Widget * container);
    WidgetDrawCommands& PushClip(const StencilPushConstants& info);
    WidgetDrawCommands& PopClip();
    
    std::vector<CommandInfo> GetCommands() const;
    std::vector<ClipInfo> GetClips() const;
    std::map<uint32_t,uint32_t> GetClipBreaks() const;

    bool Empty() const;
};

template <typename T, typename ... TArgs>
std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<WidgetDrawCommand, T>, WidgetDrawCommands&>
WidgetDrawCommands::Add(TArgs&&... args)
{
    return Add(newShared<T>(std::forward<TArgs>(args)...));
}
