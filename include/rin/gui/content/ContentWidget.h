#pragma once
#include "rin/gui/Widget.h"
namespace rin::gui
{
    class ContentWidget : public Widget
    {

    protected:
        virtual void CollectContent(DrawCommands& drawCommands,const TransformInfo& info) = 0;
    public:
        

        void Collect(DrawCommands& drawCommands,const TransformInfo& info) override;
    };
}
