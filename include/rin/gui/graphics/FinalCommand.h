#pragma once
#include "CustomCommand.h"
#include "IBatch.h"
#include "StencilClip.h"
#include "UtilCommand.h"
namespace rin::gui
{
    struct FinalCommand
    {
        IBatch * batch{nullptr};
        uint32_t mask{0x1};
        Shared<CustomCommand> custom{};
        Shared<UtilCommand> util{};
        std::vector<StencilClip> clips{};
        CommandType type{};
    };
}
