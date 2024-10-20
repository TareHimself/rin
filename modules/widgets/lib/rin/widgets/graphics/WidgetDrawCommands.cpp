#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

#include "rin/core/utils.hpp"
#include "rin/widgets/Widget.hpp"
#include "rin/widgets/WidgetSurface.hpp"


WidgetDrawCommands& WidgetDrawCommands::Add(const Shared<WidgetDrawCommand>& command)
{
    RawCommandInfo cmdInfo{command, _clipId};

    _commands.push_back(cmdInfo);

    if (!_uniqueClipStacks.contains(cmdInfo.clipId))
    {
        _uniqueClipStacks.emplace(cmdInfo.clipId, _clipStack);
    }
    return *this;
}

WidgetDrawCommands& WidgetDrawCommands::PushClip(const TransformInfo& transform, const Widget* container)
{
    auto padding = container->GetPadding();
    return PushClip(transform.transform.Translate(Vec2(padding.left, padding.top)), container->GetContentSize());
}

WidgetDrawCommands& WidgetDrawCommands::PushClip(const Matrix3<float>& transform, const Vec2<float>& size)
{
    auto id = static_cast<uint32_t>(_clips.size());

    ClipInfo clipInfo{id, transform, size};
    _clips.push_back(clipInfo);
    _clipStack.push_back(clipInfo.id);
    _clipId += std::to_string(id);
    return *this;
}


WidgetDrawCommands& WidgetDrawCommands::PopClip()
{
    auto asStr = std::to_string(_clipStack.back());
    _clipStack.pop_back();
    _clipId = _clipId.substr(0, _clipId.size() - asStr.size());
    return *this;
}

std::vector<RawCommandInfo>& WidgetDrawCommands::GetCommands()
{
    return _commands;
}

std::vector<ClipInfo>& WidgetDrawCommands::GetClips()
{
    return _clips;
}

std::map<std::string, std::deque<uint32_t>>& WidgetDrawCommands::GetUniqueClipStacks()
{
    return _uniqueClipStacks;
}

bool WidgetDrawCommands::Empty() const
{
    return _commands.empty();
}
