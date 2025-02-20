#include "rin/gui/content/ContentWidget.h"
namespace rin::gui
{
    void ContentWidget::Collect(DrawCommands& drawCommands,const TransformInfo& info)
    {
        if(!IsVisible())
        {
            return;
        }

        auto padding = GetPadding();
        CollectContent(drawCommands,{info.transform.Translate({padding.left,padding.right}),info.clip});
    }
}
