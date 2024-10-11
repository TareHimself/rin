#include "rin/widgets/slots/PanelWidgetSlot.hpp"

#include "rin/widgets/containers/PanelWidget.hpp"

PanelWidgetSlot::PanelWidgetSlot(PanelWidget* panel, const Shared<Widget>& widget) : ContainerWidgetSlot(panel, widget)
{
}
