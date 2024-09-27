#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

#include "rin/core/utils.hpp"
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/WidgetSurface.hpp"

WidgetDrawCommands& WidgetDrawCommands::Add(const Shared<WidgetDrawCommand>& command)
{
    CommandInfo cmdInfo{command,_clipStack};
    
    if(_commands.empty())
    {
        _commands.push_back(cmdInfo);
    }
    else
    {
        auto last = _commands.at(_commands.size() - 1);
        if(last.clipStack != cmdInfo.clipStack  || !last.command->CombineWith(command))
        {
            _commands.push_back(cmdInfo);
        }
    }
    
    return *this;
}

WidgetDrawCommands& WidgetDrawCommands::PushClip(const TransformInfo& transform, const Widget* container)
{
    return PushClip(StencilPushConstants{transform.transform,container->GetSurface()->GetProjection(),container->GetDrawSize(),Vec4{0.0f}});
}

WidgetDrawCommands& WidgetDrawCommands::PushClip(const StencilPushConstants& info)
{
    ClipInfo clipInfo{info};
    _clips.push_back(clipInfo);
    _clipStack.push(_clips.size());
    return *this;
}

WidgetDrawCommands& WidgetDrawCommands::PopClip()
{
    if(const auto id = _clipStack.top(); id != 0 && id % 32 == 0)
    {
        _breaks.emplace(id,_commands.size() - 1);
    }
    _clipStack.pop();
    return *this;
}

std::vector<CommandInfo> WidgetDrawCommands::GetCommands() const
{
    return _commands;
}

std::vector<ClipInfo> WidgetDrawCommands::GetClips() const
{
    return _clips;
}

std::map<uint32_t,uint32_t> WidgetDrawCommands::GetClipBreaks() const
{
    return _breaks;
}

bool WidgetDrawCommands::Empty() const
{
    return _commands.empty();
}
